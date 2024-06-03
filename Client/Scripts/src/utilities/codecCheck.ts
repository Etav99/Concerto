const VIDEO_EXTENSIONS = ["webm", "mp4", "ogg", "x-matroska"];
const AUDIO_EXTENSIONS = ["webm", "mp3", "ogg", "mp4", "wav", "flac", "aac", "ac3", "eac3", "mp2", "dts"];
const VIDEO_CODECS = ["vp9", "vp9.0", "vp8", "vp8.0", "avc1", "av1", "h265", "h.265", "h264", "h.264", "daala"];
const AUDIO_CODECS = ["opus", "vorbis", "mp3", "aac", "mp4a", "ac3", "eac3", "mp2", "dts", "flac", "pcm"];

export function getAnySupportedVideoMimeTypeAndExtension(): [string, string]   {
    let types: [string, string][] = [];
    for(let ext of VIDEO_EXTENSIONS) {
        for(let vCodec of VIDEO_CODECS) {
          for(let aCodec of AUDIO_CODECS) {
            types.push([`video/${ext};codecs=${vCodec},${aCodec}`, ext]);
          }
          types.push([`video/${ext};codecs=${vCodec}`, ext]);
        }
        for(let aCodec of AUDIO_CODECS) {
            types.push([`video/${ext};codecs=${aCodec}`, ext]);
        }
        types.push([`video/${ext}`, ext]);
    }

    for (const type of types) {
        if (isTypeSupported(type[0]))
        {
            console.log(`Found supported MediaRecorder video mimeType: ${type[0]}`);
            return type;
        }
    }
    throw new Error("Browser doesn't support any of the video codecs");
}

export function getAnySupportedAudioMimeTypeAndExtension(): [string, string]   {
    let types: [string, string][] = [];
    for(let ext of AUDIO_EXTENSIONS) {
        for(let aCodec of AUDIO_CODECS) {
          types.push([`audio/${ext};codecs=${aCodec}`, ext]);
        }
        types.push([`audio/${ext}`, ext]);
    }
    for (const type of types) {
        if (isTypeSupported(type[0]))
        {
            console.log(`Found supported MediaRecorder audio mimeType: ${type[0]}`);
            return type;
        }
    }
    throw new Error("Browser doesn't support any of the audio codecs");
}


function isTypeSupported(type: string) {
    if(!MediaRecorder.isTypeSupported(type)) return false;
    try
    {
        var recorder = new MediaRecorder(new MediaStream(), {mimeType: type});
        // dispose the recorder
        return true;
    }
    catch (e)
    {
        return false;
    }
}