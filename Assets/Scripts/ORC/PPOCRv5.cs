using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Unity.InferenceEngine;
using UnityEngine;

public class PPOCRv5 : MonoBehaviour
{
    public Unity.InferenceEngine.ModelAsset modelAsset;
    public TextAsset yamlConfig;
    public Unity.InferenceEngine.BackendType backend = Unity.InferenceEngine.BackendType.GPUCompute;

    const int targetHeight = 48;
    const int maxWidth = 320;

    const string inputName = "x";
    const string outputName = "fetch_name_0";

    Unity.InferenceEngine.Model runtimeModel;
    Unity.InferenceEngine.Worker worker;
    string[] charDict;

    void Start()
    {
        if (modelAsset == null || yamlConfig == null) return;
        runtimeModel = Unity.InferenceEngine.ModelLoader.Load(modelAsset);
        worker = new Unity.InferenceEngine.Worker(runtimeModel, backend);
        charDict = ParseCharacterDictFromYaml(yamlConfig.text);
        Debug.Log($"Loaded character dict length = {charDict?.Length ?? 0}");
    }

    public string RunOCR(Texture2D tex)
    {
        float scale = (float)targetHeight / tex.height;
        int scaledW = Mathf.CeilToInt(tex.width * scale);
        int inputW = Mathf.Min(maxWidth, Math.Max(1, scaledW));

        var inputShape = new Unity.InferenceEngine.TensorShape(1, 3, targetHeight, inputW);
        using var inputTensor = new Unity.InferenceEngine.Tensor<float>(inputShape);

        var transform = new TextureTransform().SetDimensions(inputW, targetHeight, 3).SetChannelSwizzle(ChannelSwizzle.BGRA).SetTensorLayout(Unity.InferenceEngine.TensorLayout.NCHW);
        Unity.InferenceEngine.TextureConverter.ToTensor(tex, inputTensor, transform);
        worker.Schedule(inputTensor);
        var gpuOut = worker.PeekOutput(outputName) as Unity.InferenceEngine.Tensor<float>;
        if (gpuOut == null)
        {
            Debug.LogError("找不到输出 tensor，请确认输出名称是否正确: " + outputName);
            return "";
        }

        using var cpuOut = gpuOut.ReadbackAndClone();
        float[] outArr = cpuOut.DownloadToArray();
        var outShape = cpuOut.shape;

        string result = CTCDecodeFromLogits(outArr, outShape, charDict);

        return result;
    }

    string[] ParseCharacterDictFromYaml(string yamlText)
    {
        if (string.IsNullOrEmpty(yamlText)) return null;

        var dictMatch = Regex.Match(yamlText, @"character_dict:\s*\n(?<items>(\s*-\s*.*\n)+)", RegexOptions.Multiline);
        if (!dictMatch.Success)
        {
            var fallbackMatches = Regex.Matches(yamlText, @"^\s*-\s*(.+)$", RegexOptions.Multiline);
            List<string> fallback = new();
            foreach (Match m in fallbackMatches)
            {
                string val = m.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(val)) fallback.Add(val);
            }
            return fallback.Count > 0 ? fallback.ToArray() : null;
        }

        string itemsBlock = dictMatch.Groups["items"].Value;
        var lines = itemsBlock.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var list = new List<string>();
        foreach (var rawLine in lines)
        {
            var m = Regex.Match(rawLine, @"-\s*(.*)$");
            if (m.Success)
            {
                string v = m.Groups[1].Value.Trim();
                if ((v.StartsWith("'") && v.EndsWith("'")) || (v.StartsWith("\"") && v.EndsWith("\"")))
                    v = v.Substring(1, v.Length - 2);
                list.Add(v);
            }
        }
        return list.ToArray();
    }

    string CTCDecodeFromLogits(float[] rawData, Unity.InferenceEngine.TensorShape shape, string[] dict)
    {
        if (dict == null || dict.Length == 0) return "";

        int batch = shape[0];
        if (batch != 1) Debug.LogWarning($"batch=={batch}, 仅解码第0个样本");

        int d1 = shape[1];
        int d2 = shape[2];

        int seqLen, numClasses;
        float[] logits;

        if (d1 == dict.Length)
        {
            numClasses = d1;
            seqLen = d2;
            logits = new float[seqLen * numClasses];
            for (int c = 0; c < numClasses; c++)
                for (int t = 0; t < seqLen; t++)
                    logits[t * numClasses + c] = rawData[c * seqLen + t];
        }
        else
        {
            seqLen = d1;
            numClasses = d2;
            logits = rawData;
        }

        var sb = new StringBuilder();
        int prev = -1;
        for (int t = 0; t < seqLen; t++)
        {
            int argmax = 0;
            float maxv = float.MinValue;
            for (int c = 0; c < numClasses; c++)
            {
                float v = logits[t * numClasses + c];
                if (v > maxv) { maxv = v; argmax = c; }
            }

            if (argmax != prev && argmax != 0)
            {
                int dictIndex = argmax - 1;
                if (dictIndex >= 0 && dictIndex < dict.Length)
                    sb.Append(dict[dictIndex]);
                else
                    sb.Append($"[#{argmax}]");
            }
            prev = argmax;
        }

        return sb.ToString();
    }

    void OnDestroy()
    {
        worker?.Dispose();
        runtimeModel = null;
    }
}
