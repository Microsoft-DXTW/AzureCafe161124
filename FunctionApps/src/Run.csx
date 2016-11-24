#r "Newtonsoft.Json"

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

// 請到 http://microsoft.com/cognitive 申請 Face API 金鑰
const string FACEAPI_KEY = "<從 Microsoft Cognitive Service 申請>";

public static async Task Run(byte[] image, string filename, Stream outputBlob, IAsyncCollector<Face> demoTable, TraceWriter log)
{
    using (var ms = new MemoryStream(image))
    {
        outputBlob.Write(image, 0, image.Length);
    }

    string result = await AnalyzeImageAsync(image);
    log.Info(result);

    if (String.IsNullOrEmpty(result))
    {
        return;
    }

    ImageData imageData = JsonConvert.DeserializeObject<ImageData>(result);
    foreach (Face face in imageData.Faces)
    {
        face.PartitionKey = "ProfileImageUpload";
        face.RowKey = Guid.NewGuid().ToString();
        await demoTable.AddAsync(face);
    }
}

static async Task<string> AnalyzeImageAsync(byte[] image)
{
    using (var client = new HttpClient())
    {
        var content = new StreamContent(new MemoryStream(image));
        var url = "https://api.projectoxford.ai/vision/v1.0/analyze?visualFeatures=Faces";
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", FACEAPI_KEY);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        
        var httpResponse = await client.PostAsync(url, content);
        if (httpResponse.StatusCode == HttpStatusCode.OK)
        {
            return await httpResponse.Content.ReadAsStringAsync();
        }
    }
    return null;
}

public class ImageData
{
    public Face[] Faces { get; set; }
}

public class Face
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; }
}