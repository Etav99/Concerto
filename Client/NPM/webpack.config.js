const path = require("path");
const webpack = require('webpack');

module.exports = {

   entry: {
    bundle: './src/bundle.js',
    worker: './src/videoFileWorker.js'
  },
  output: {
     path: path.resolve(__dirname, '/dist'),
     filename: '[name].js'
  }
};