import { initializeRecordingManager, RecordingManager, getVideoInputs, getAudioInputs} from './recorder.ts';
import { disablePreventWindowClose, enablePreventWindowClose} from './preventWindowClose';
import { isMobile } from 'is-mobile';

window.initializeRecordingManager = initializeRecordingManager;
window.getVideoInputs = getVideoInputs;
window.getAudioInputs = getAudioInputs;
window.isMobile = isMobile;
window.disablePreventWindowClose = disablePreventWindowClose;
window.enablePreventWindowClose = enablePreventWindowClose;
window.RecordingManager = RecordingManager;