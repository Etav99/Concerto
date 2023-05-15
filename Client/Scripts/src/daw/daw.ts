import { init as initWaveformPlaylist } from '../waveform-playlist/src/app.js';
import PlaylistEvents from '../waveform-playlist/src/PlaylistEvents';

import './css/main.css';
import '@fortawesome/fontawesome-free/css/all.min.css';
import 'bootstrap/dist/css/bootstrap.min.css';

import { EventEmitter } from "eventemitter3";

import controlsHTML from './controls.html';

const recordingConstraints = { audio: true };





export class Daw {
  private tracks: DawTrack[];

  private playlist: any;
  private eventEmitter: EventEmitter;

  public static async initialize(containerId: string): Promise<Daw> {
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
    if (!container)
      throw new Error("Could not find container with id: " + containerId);
    var options = {
      samplesPerPixel: 3000,
      waveHeight: 100,
      container: container,
      state: 'cursor',
      colors: {
        waveOutlineColor: '#005BBB',
        timeColor: 'grey',
        fadeColor: 'black'
      },
      timescale: true,
      controls: {
        show: false, //whether or not to include the track controls
        width: 200 //width of controls in pixels
      },
      seekStyle: 'line',
      zoomLevels: [100, 250, 500, 1000, 3000, 5000]
    }

    let daw = new Daw(options, microphoneStream);
    // await daw.loadSamplePlaylist();
    // await daw.playlist.createTrack("test");

    // add event listeners to the UI
    let ee = daw.eventEmitter;

    // async timeout 
    new Promise(
      () => setTimeout(
        () => {
          daw.playlist.updateTrack(
            "Drums",
            {
              src: "assets/BassDrums30.mp3",
              name: "Drums",
              gain: 0.75,
              start: 7,
            }
          )
        },
        4000
      )
    );

    return daw;
  }

  public constructor(options: {}, userMediaStream: MediaStream) {
    this.eventEmitter = new EventEmitter();
    this.playlist = initWaveformPlaylist(options, this.eventEmitter);
    this.playlist.initRecorder(userMediaStream);
  }

  public async updateTrackList (trackList: DawTrack[]) {


  }

  public play() {
    this.eventEmitter.emit(PlaylistEvents.PLAY);
  }

  public pause() {
    this.eventEmitter.emit(PlaylistEvents.PAUSE);
  }

  public stop() {
    this.eventEmitter.emit(PlaylistEvents.STOP);
  }

  public record() {
    this.eventEmitter.emit(PlaylistEvents.RECORD);
  }

  public fastForward() {
    this.eventEmitter.emit(PlaylistEvents.FAST_FORWARD);
  }

  public rewind() {
    this.eventEmitter.emit(PlaylistEvents.REWIND);
  }

  public zoomIn() {
    this.eventEmitter.emit(PlaylistEvents.ZOOM_IN);
  }

  public zoomOut() {
    this.eventEmitter.emit(PlaylistEvents.ZOOM_OUT);
  }

  public setSelectState() {
    this.eventEmitter.emit(PlaylistEvents.STATE_CHANGE, "select");
  }

  public setShiftState() {
    this.eventEmitter.emit(PlaylistEvents.STATE_CHANGE, "shift");
  }

  public setCursorState() {
    this.eventEmitter.emit(PlaylistEvents.STATE_CHANGE, "cursor");
  }

  public async loadSamplePlaylist() {
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
    await this.playlist.loadTrackList(trackList);
  }

  public static initDevControls(containerId: string) {
    let container = document.getElementById(containerId);
    // generate UI for the playlist
    container.insertAdjacentHTML('afterbegin', controlsHTML);
}

}

function toggleActive(node: HTMLElement) {
  var active = node.parentNode.querySelectorAll('.active');
  for (var i = 0; i < active.length; i++)
    active[i].classList.remove('active');
  node.classList.toggle('active');
}

