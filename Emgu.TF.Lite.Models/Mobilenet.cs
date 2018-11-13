﻿//----------------------------------------------------------------------------
//  Copyright (C) 2004-2018 by EMGU Corporation. All rights reserved.       
//----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Emgu.TF.Lite;
using Emgu.Models;
using System.IO;
using System.ComponentModel;
using System.Net;

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
using UnityEngine;
#endif

namespace Emgu.TF.Lite.Models
{
    public class Mobilenet : Emgu.TF.Util.UnmanagedObject
    {
        private FileDownloadManager _downloadManager;

        //private BuildinOpResolver _resolver = null;
        private Interpreter _interpreter = null;
        private String[] _labels = null;
        private FlatBufferModel _model = null;
        private Tensor _inputTensor;
        private Tensor _outputTensor;

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
        public double DownloadProgress
        {
            get
            {
                if (_downloadManager == null)
                    return 0;
                if (_downloadManager.CurrentWebClient == null)
                    return 1;
                return _downloadManager.CurrentWebClient.downloadProgress;
            }
        }

        public String DownloadFileName
        {
            get
            {
                if (_downloadManager == null)
                    return null;
                if (_downloadManager.CurrentWebClient == null)
                    return null;
                return _downloadManager.CurrentWebClient.url;
            }
        }
#endif

        public Mobilenet()
        {
            _downloadManager = new FileDownloadManager();

            _downloadManager.OnDownloadProgressChanged += onDownloadProgressChanged;
            _downloadManager.OnDownloadCompleted += onDownloadCompleted;

                
        }

        private void onDownloadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            ImportGraph();
            if (OnDownloadCompleted != null)
            {
                OnDownloadCompleted(sender, e);
            }
        }

        private void onDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            if (OnDownloadProgressChanged != null)
                OnDownloadProgressChanged(sender, e);
        }

        public event System.Net.DownloadProgressChangedEventHandler OnDownloadProgressChanged;
        public event System.ComponentModel.AsyncCompletedEventHandler OnDownloadCompleted;


        public
#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
            IEnumerator
#else
            void
#endif
            Init(String[] modelFiles = null, String downloadUrl = null)
        {

            _downloadManager.Clear();
            String url = downloadUrl == null ? "https://github.com/emgucv/models/raw/master/mobilenet_v1_1.0_224_float_2017_11_08/" : downloadUrl;
            String[] fileNames = modelFiles == null ? new string[] { "mobilenet_v1_1.0_224.tflite", "labels.txt" } : modelFiles;
            for (int i = 0; i < fileNames.Length; i++)
                _downloadManager.AddFile(url + fileNames[i]);

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
            yield return _downloadManager.Download();
#else
            _downloadManager.Download();
#endif
        }

        public bool Imported
        {
            get
            {
                return _interpreter != null;
            }
        }

        private void ImportGraph()
        {
            if (_interpreter != null)
                _interpreter.Dispose();

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
            UnityEngine.Debug.Log("Reading model definition");
#endif

            String modelFileName = _downloadManager.Files[0].LocalFile;
            String labelFileName = _downloadManager.Files[1].LocalFile;

            System.Diagnostics.Debug.Assert(File.Exists(modelFileName), String.Format("File {0} doesn't exist", modelFileName));
            System.Diagnostics.Debug.Assert(File.Exists(labelFileName), String.Format("File {0} doesn't exist", labelFileName));

            if (!File.Exists(modelFileName) || !File.Exists(labelFileName))
                return;

            if (_labels == null)
                _labels = File.ReadAllLines(labelFileName);

            if (_model == null)
            {
                _model = new FlatBufferModel(modelFileName);
                if (!_model.CheckModelIdentifier())
                    throw new Exception("Model indentifier check failed");
            }

            if (_interpreter == null)
            {
                _interpreter = new Interpreter(_model);
                Status allocateTensorStatus = _interpreter.AllocateTensors();
                if (allocateTensorStatus == Status.Error)
                    throw new Exception("Failed to allocate tensor");
            }

            if (_inputTensor == null)
            {

                int[] input = _interpreter.InputIndices;
                _inputTensor = _interpreter.GetTensor(input[0]);
                
            }

            if (_outputTensor == null)
            {
                int[] output = _interpreter.OutputIndices;
                _outputTensor = _interpreter.GetTensor(output[0]);   
            }
        }

        
        public Interpreter Interpreter
        {
            get
            {
                return _interpreter;
            }
        }

        public String[] Labels
        {
            get { return _labels; }
        }

        public RecognitionResult[] SortResults(float[] probabilities)
        {
            if (probabilities == null)
                return null;

            RecognitionResult[] results = new RecognitionResult[probabilities.Length];
            for (int i = 0; i < probabilities.Length; i++)
            {
                results[i] = new RecognitionResult(_labels[i], probabilities[i]);
            }
            Array.Sort<RecognitionResult>(results, new Comparison<RecognitionResult>((a, b) => -a.Probability.CompareTo(b.Probability)));
            return results;
        }

#if UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE
        public RecognitionResult[] Recognize(Texture2D texture2D, bool flipUpsideDown=true, bool swapBR = false)
        {
            NativeImageIO.ReadTensorFromTexture2D<float>(
                texture2D, 
                _inputTensor.DataPointer, 
                224, 224, 128.0f, 1.0f / 128.0f,
                flipUpsideDown,
                swapBR);

            _interpreter.Invoke();

            float[] probability = _outputTensor.GetData(false) as float[];
            if (probability == null)
                return null;

            return SortResults(probability);

        }
#else
        public RecognitionResult[] Recognize(String imageFile)
        {
            NativeImageIO.ReadImageFileToTensor<float>(imageFile, _inputTensor.DataPointer, 224, 224, 128.0f, 1.0f / 128.0f);

            _interpreter.Invoke();

            float[] probability = _outputTensor.Data as float[];
            if (probability == null)
                return null;
            return SortResults(probability);

        }

#endif

        public class RecognitionResult
        {
            public RecognitionResult(String label, double probability)
            {
                Label = label;
                Probability = probability;
            }

            public String Label;
            public double Probability;
        }

        protected override void DisposeObject()
        {
            
            if (_interpreter != null)
            {
                _interpreter.Dispose();
                _interpreter = null;
            }

            if (_model != null)
            {
                _model.Dispose();
                _model = null;
            }
        }
    }
}
