const { type } = require("os");
const path = require("path");
const webpack = require('webpack');

module.exports = {
    entry: {
        bundle: './src/bundle.js'
    },
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          use: 'ts-loader',
          exclude: ["/node_modules/", "/src/waveform-playlist/node_modules/" ], 
        },
        {
          test: /\.css$/,
          use: [
            'style-loader',
            'css-loader',
            {
              loader: 'postcss-loader',
              options: {
                postcssOptions: {
                  plugins: [
                    [
                      'postcss-prefix-selector',
                      {
                        prefix: '.daw',
                        transform: function (prefix, selector, prefixedSelector) {
                          return prefixedSelector;
                        },
                      },
                    ],
                  ],
                },
              },
            },
          ]
        },
        {
          test: /\.html$/,
          type: 'asset/source'
        },
      ],
    },
    plugins: [
      new webpack.ProvidePlugin({
        $: 'jquery',
        jQuery: 'jquery'
      })
    ],
    resolve: {
      extensions: ['.tsx', '.ts', '.js', '.css', '.html'],
    },
    output: {
        path: path.resolve(__dirname, 'dist'),
        filename: '[name].js'
    },
    node: {
      global: true
    }

  };