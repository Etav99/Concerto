import { init as initWaveformPlaylist } from '../waveform-playlist/src/app.js';
import PlaylistEvents from '../waveform-playlist/src/PlaylistEvents';
import { EventEmitter } from "eventemitter3";

import './css/main.css';
import '@fortawesome/fontawesome-free/css/all.min.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import controlsHTML from './controls.html';

const recordingConstraints = { audio: true };

export async function initializeDaw(containerId: string, options: any, dotNetReference: any = null): Promise<any> {
  var container = document.getElementById(containerId);
  if (!container) throw new Error("Could not find container with id: " + containerId);

  options.container = container;

  const eventEmitter = new EventEmitter();

  const playlist = initWaveformPlaylist(options, eventEmitter);

  if(dotNetReference != null) {
    const onShift = (trackId: number, startTime: number) => dotNetReference.invokeMethodAsync('OnShift', trackId, startTime);
    const recordingFinished = (trackId: number, blob: Blob) => {
      const blobUrl = URL.createObjectURL(blob);
      dotNetReference.invokeMethodAsync('OnRecordingFinished', trackId, blobUrl);
    }

    // const onVolumeChange = (track: number, volume: number) => dotNetReference.invokeMethodAsync('OnVolumeChange', track, volume);
    // TODO add these to playlist events
    eventEmitter.on(PlaylistEvents.TRACK_START_TIME_UPDATE, onShift);
    eventEmitter.on(PlaylistEvents.RECORDING_FINISHED, recordingFinished);
    // eventEmitter.on(PlaylistEvents.VOLUME_CHANGE, onVolumeChange);

  }

  return playlist;
}
