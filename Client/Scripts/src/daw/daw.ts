import { init as initWaveformPlaylist } from '../waveform-playlist/src/app.js';
import PlaylistEvents from '../waveform-playlist/src/PlaylistEvents';
import { EventEmitter } from "eventemitter3";

import './css/main.css';
import '@fortawesome/fontawesome-free/css/all.min.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import controlsHTML from './controls.html';

const recordingConstraints = { audio: true };

export async function initializeDaw(containerId: string, options: any, dotNetReference: any = null): Promise<any> {
  // get microphone input stream
  let microphoneStream: MediaStream;
  try {
    microphoneStream = await navigator.mediaDevices.getUserMedia(recordingConstraints)
  }
  catch (err) {
    console.log(err);
    return;
  }

  var container = document.getElementById(containerId);
  if (!container) throw new Error("Could not find container with id: " + containerId);

  options.container = container;

  const eventEmitter = new EventEmitter();

  const playlist = initWaveformPlaylist(options, eventEmitter);
  playlist.initRecorder(microphoneStream);

  if(dotNetReference != null) {
    const onShift = (trackId: number, startTime: number) => dotNetReference.invokeMethodAsync('OnShift', trackId, startTime);
    // const onVolumeChange = (track: number, volume: number) => dotNetReference.invokeMethodAsync('OnVolumeChange', track, volume);
    // TODO add these to playlist events
    eventEmitter.on(PlaylistEvents.TRACK_START_TIME_UPDATE, onShift);
    // eventEmitter.on(PlaylistEvents.VOLUME_CHANGE, onVolumeChange);

  }

  return playlist;
}


export async function loadSamplePlaylist(playlist: any) {
  const trackList = [
    {
      src: "assets/BassDrums30.mp3",
      name: "Drums",
      gain: 0.5,
    },
    {
      src: "assets/Guitar30.mp3",
      name: "Guitar",
      gain: 0.5,
    },
  ];
  await playlist.loadTrackList(trackList);
}

export function initDevControls(containerId: string) {
  let container = document.getElementById(containerId);
  // generate UI for the playlist
  container.insertAdjacentHTML('afterbegin', controlsHTML);
}