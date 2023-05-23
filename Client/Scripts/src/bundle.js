import { initializeRecordingManager, RecordingManager, getVideoInputs, getAudioInputs} from './recorder/recorder.ts';
import { disablePreventWindowClose, enablePreventWindowClose} from './utilities/preventWindowClose.ts';
import { initializeDaw } from './daw/daw.ts';
import { isMobile } from 'is-mobile';

const window = global.window;

// Utilities
window.disablePreventWindowClose = disablePreventWindowClose;
window.enablePreventWindowClose = enablePreventWindowClose;
window.isMobile = isMobile;
window.getVideoInputs = getVideoInputs;
window.getAudioInputs = getAudioInputs;

// Video recorder
window.initializeRecordingManager = initializeRecordingManager;
window.RecordingManager = RecordingManager;

// DAW
window.initializeDaw = initializeDaw;
