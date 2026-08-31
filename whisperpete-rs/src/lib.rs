#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use anyhow::{Context, Result};
use arboard::Clipboard;
use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use serde::Serialize;
use sherpa_onnx::{OfflineRecognizer, OfflineRecognizerConfig, OfflineTransducerModelConfig};
use std::{
    path::PathBuf,
    sync::{Arc, Mutex},
};
use tauri::{
    AppHandle, Emitter, Manager, State,
    menu::{Menu, MenuItem},
    tray::TrayIconBuilder,
};
use tauri_plugin_global_shortcut::{GlobalShortcutExt, ShortcutState};
use tauri_plugin_opener::OpenerExt;

#[derive(Default)]
struct RecordingState(Arc<Mutex<Option<Recording>>>);

struct Recording {
    samples: Arc<Mutex<Vec<f32>>>,
    stream: cpal::Stream,
    sample_rate: u32,
}

#[derive(Serialize)]
struct Status {
    recording: bool,
    model_dir: String,
}

#[tauri::command]
fn status(state: State<'_, RecordingState>) -> Status {
    Status {
        recording: state.0.lock().expect("recording state poisoned").is_some(),
        model_dir: model_dir().display().to_string(),
    }
}

#[tauri::command]
fn start_recording(state: State<'_, RecordingState>) -> Result<(), String> {
    let mut guard = state.0.lock().map_err(|e| e.to_string())?;
    if guard.is_some() {
        return Ok(());
    }
    *guard = Some(capture_audio().map_err(|e| format!("Unable to start microphone: {e:#}"))?);
    Ok(())
}

#[tauri::command]
async fn stop_recording(state: State<'_, RecordingState>) -> Result<String, String> {
    stop_recording_shared(state.0.clone()).await
}

async fn stop_recording_shared(shared: Arc<Mutex<Option<Recording>>>) -> Result<String, String> {
    let recording = shared
        .lock()
        .map_err(|e| e.to_string())?
        .take()
        .ok_or_else(|| "No recording is active".to_string())?;
    tauri::async_runtime::spawn_blocking(move || finish_recording(recording))
        .await
        .map_err(|e| format!("Transcription worker failed: {e}"))?
}

fn toggle_recording(app: &AppHandle) {
    let state = app.state::<RecordingState>();
    if state.0.lock().map(|g| g.is_some()).unwrap_or(false) {
        let shared = state.0.clone();
        let app = app.clone();
        tauri::async_runtime::spawn(async move {
            match stop_recording_shared(shared).await {
                Ok(text) => {
                    let _ = app.emit("recording-result", text);
                }
                Err(error) => {
                    let _ = app.emit("recording-error", error);
                }
            }
            let _ = app.emit("recording-state", false);
        });
    } else {
        match start_recording(state) {
            Ok(()) => {
                let _ = app.emit("recording-state", true);
            }
            Err(error) => {
                let _ = app.emit("recording-error", error);
            }
        }
    }
}

fn finish_recording(recording: Recording) -> Result<String, String> {
    drop(recording.stream);
    let samples = recording.samples.lock().map_err(|e| e.to_string())?.clone();
    if samples.len() < recording.sample_rate as usize / 10 {
        return Err("Recording was too short".to_string());
    }
    let transcript = transcribe(&samples, recording.sample_rate)
        .map_err(|e| format!("Transcription failed: {e:#}"))?;
    if transcript.trim().is_empty() {
        return Err("No speech was detected".to_string());
    }
    copy_to_clipboard(&transcript).map_err(|e| format!("Clipboard copy failed: {e:#}"))?;
    Ok(transcript)
}

fn model_dir() -> PathBuf {
    std::env::var_os("WHISPERPETE_MODEL_DIR")
        .map(PathBuf::from)
        .unwrap_or_else(|| {
            std::env::var_os("LOCALAPPDATA")
                .map(PathBuf::from)
                .unwrap_or_else(|| PathBuf::from("."))
                .join("WhisperPete")
                .join("models")
                .join("sherpa-onnx-nemo-parakeet-tdt-0.6b-v3-int8")
        })
}

fn capture_audio() -> Result<Recording> {
    let host = cpal::default_host();
    let device = host
        .default_input_device()
        .context("no default input device")?;
    let supported = device
        .default_input_config()
        .context("no default input configuration")?;
    let sample_rate = supported.sample_rate();
    let channels = supported.channels() as usize;
    let samples = Arc::new(Mutex::new(Vec::new()));
    let sink = Arc::clone(&samples);
    let err_fn = |err| eprintln!("audio stream error: {err}");
    let stream = match supported.sample_format() {
        cpal::SampleFormat::F32 => device.build_input_stream(
            supported.clone().into(),
            move |data: &[f32], _| append_samples(data, channels, &sink),
            err_fn,
            None,
        )?,
        cpal::SampleFormat::I16 => device.build_input_stream(
            supported.clone().into(),
            move |data: &[i16], _| append_i16(data, channels, &sink),
            err_fn,
            None,
        )?,
        cpal::SampleFormat::U16 => device.build_input_stream(
            supported.into(),
            move |data: &[u16], _| append_u16(data, channels, &sink),
            err_fn,
            None,
        )?,
        format => anyhow::bail!("unsupported microphone format: {format:?}"),
    };
    stream.play()?;
    Ok(Recording {
        samples,
        stream,
        sample_rate,
    })
}

fn append_samples(data: &[f32], channels: usize, sink: &Arc<Mutex<Vec<f32>>>) {
    let mut out = sink.lock().expect("audio buffer poisoned");
    out.extend(
        data.chunks(channels)
            .map(|frame| frame.iter().copied().sum::<f32>() / channels as f32),
    );
}
fn append_i16(data: &[i16], channels: usize, sink: &Arc<Mutex<Vec<f32>>>) {
    let converted: Vec<f32> = data.iter().map(|v| *v as f32 / i16::MAX as f32).collect();
    append_samples(&converted, channels, sink);
}
fn append_u16(data: &[u16], channels: usize, sink: &Arc<Mutex<Vec<f32>>>) {
    let converted: Vec<f32> = data
        .iter()
        .map(|v| (*v as f32 - 32768.0) / 32768.0)
        .collect();
    append_samples(&converted, channels, sink);
}

fn transcribe(samples: &[f32], sample_rate: u32) -> Result<String> {
    let dir = model_dir();
    let files = [
        "encoder.int8.onnx",
        "decoder.int8.onnx",
        "joiner.int8.onnx",
        "tokens.txt",
    ];
    for file in files {
        if !dir.join(file).is_file() {
            anyhow::bail!("missing model file {} in {}", file, dir.display());
        }
    }
    let mut config = OfflineRecognizerConfig::default();
    config.model_config.transducer = OfflineTransducerModelConfig {
        encoder: Some(dir.join("encoder.int8.onnx").display().to_string()),
        decoder: Some(dir.join("decoder.int8.onnx").display().to_string()),
        joiner: Some(dir.join("joiner.int8.onnx").display().to_string()),
    };
    config.model_config.tokens = Some(dir.join("tokens.txt").display().to_string());
    config.model_config.model_type = Some("nemo_transducer".into());
    config.model_config.provider = Some("cpu".into());
    config.model_config.num_threads = 4;
    let recognizer = OfflineRecognizer::create(&config).context("create Parakeet recognizer")?;
    let stream = recognizer.create_stream();
    stream.accept_waveform(sample_rate as i32, samples);
    recognizer.decode(&stream);
    Ok(stream.get_result().context("empty recognizer result")?.text)
}

fn copy_to_clipboard(text: &str) -> Result<()> {
    let mut clipboard = Clipboard::new().context("open Windows clipboard")?;
    clipboard
        .set_text(text.to_owned())
        .context("set clipboard text")?;
    Ok(())
}

#[tauri::command]
fn open_support_url(app: AppHandle) -> Result<(), String> {
    app.opener()
        .open_url("https://buymeacoffee.com/pbeens", None::<&str>)
        .map_err(|error| format!("Could not open support link: {error}"))
}

pub fn run() {
    tauri::Builder::default()
        .manage(RecordingState::default())
        .plugin(
            tauri_plugin_global_shortcut::Builder::new()
                .with_handler(|app, _shortcut, event| {
                    if event.state() == ShortcutState::Pressed {
                        toggle_recording(app);
                    }
                })
                .build(),
        )
        .plugin(tauri_plugin_opener::init())
        .invoke_handler(tauri::generate_handler![
            status,
            start_recording,
            stop_recording,
            open_support_url
        ])
        .setup(|app| {
            let toggle = MenuItem::with_id(app, "toggle", "Toggle recording", true, None::<&str>)?;
            let quit = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&toggle, &quit])?;
            let icon_pixels = vec![0, 120, 215, 255].repeat(16 * 16);
            let icon = tauri::image::Image::new_owned(icon_pixels, 16, 16);
            TrayIconBuilder::new()
                .icon(icon)
                .menu(&menu)
                .on_menu_event(|app, event| match event.id().as_ref() {
                    "toggle" => {
                        toggle_recording(app);
                    }
                    "quit" => app.exit(0),
                    _ => {}
                })
                .build(app)?;
            app.global_shortcut().register("Alt+Shift+Space")?;
            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running WhisperPete");
}
