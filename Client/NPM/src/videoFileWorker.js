fixWebmDuration = require('webm-duration-fix').default;

const Action = {
  Start: 'b',
  Stop: 'e',
  Data: 'd',
};

// Start - [b, mimeType, extension]
// Stop - [e]
// Data - [d, data]

self.data = [];

self.mimeType = null;
self.extension = null;
self.stop = false;

onmessage = async function(e) {
    if (e.data[0] == Action.Data)
    {
        if(stop) return;
        data.push(e.data[1]);
    }
    else if (e.data[0] == Action.Start)
    {
        self.mimeType = e.data[1];
        self.extension = e.data[2];
    }
    else if (e.data[0] == Action.Stop)
    {
        // wait 500 ms for the last data to arrive
        await new Promise(r => setTimeout(r, 500));
        stop = true;
        
        let recording = new Blob(data, { type: mimeType });

        if (this.outputFileExtension = "webm")
            recording = await fixWebmDuration(recording, { type: this.mimeType });

        let RecordingUrl = URL.createObjectURL(recording);
        this.postMessage(RecordingUrl);
    }

};