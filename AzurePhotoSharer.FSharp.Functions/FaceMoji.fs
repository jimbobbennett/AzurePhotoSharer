namespace AzurePhotoSharing.FSharp.Functions

module FaceMoji =
    open System
    open System.IO
    open System.Net
    open System.Net.Http
    open System.Net.Http.Headers
    open System.Drawing
    open System.Drawing.Imaging
    open FSharp.Data
    open Microsoft.Azure.WebJobs
    open Microsoft.Azure.WebJobs.Extensions.Http
    open Microsoft.Azure.CognitiveServices.Vision.Face
    open Microsoft.Azure.CognitiveServices.Vision.Face.Models
    open Newtonsoft.Json
    open AzurePhotoSharer.Shared
    open System.Text
        
    type FaceRectangle = { Height: int; Width: int; Top: int; Left: int; }
    type Scores = { Anger: float; Contempt: float; Disgust: float; Fear: float;
                    Happiness: float; Neutral: float; Sadness: float; Surprise: float; }

    let apiKey = Environment.GetEnvironmentVariable("EmotionApiKey")
    
    let apiKeyCredentials = new ApiKeyServiceClientCredentials(apiKey)
    let faceClient = 
        let fc = new FaceClient(apiKeyCredentials)
        fc.Endpoint <- "https://westeurope.api.cognitive.microsoft.com"
        fc
        
    let getFaces (bytes:byte[]) =
        async {
            let ms = new MemoryStream(bytes)
            let attributes = new System.Collections.Generic.List<FaceAttributeType>([FaceAttributeType.Emotion])
            return faceClient.Face.DetectWithStreamAsync(ms, 
                                                         returnFaceAttributes = attributes)
                            |> Async.AwaitTask
        } |> Async.RunSynchronously

    let getEmoji (face : DetectedFace) =
        match face.FaceAttributes.Emotion with
            | scores when scores.Anger > 0.1 -> Images.angry
            | scores when scores.Fear > 0.1 -> Images.afraid
            | scores when scores.Sadness > 0.1 -> Images.sad
            | scores when scores.Happiness > 0.5 -> Images.happy
            | _ -> Images.neutral
        |> fun img -> Convert.FromBase64String(img)
        |> fun bytes -> new MemoryStream(bytes)
        |> Image.FromStream

    let drawImage (bytes: byte[]) faces =
        use inputStream = new MemoryStream(bytes)
        use image = Image.FromStream(inputStream)
        use graphics = Graphics.FromImage(image)
    
        faces |> Seq.iter(fun (face : DetectedFace) ->
            let rect = face.FaceRectangle
            let emoji = getEmoji face
            graphics.DrawImage(emoji, rect.Left, rect.Top, rect.Width, rect.Height)
        )

        use outputStream = new MemoryStream();
        image.Save(outputStream, ImageFormat.Jpeg)
        outputStream.ToArray()

    let createResponse bytes =
        let photo = new Photo()
        photo.PhotoBase64 <- Convert.ToBase64String(bytes)
        let content = JsonConvert.SerializeObject(photo)
        let response = new HttpResponseMessage()
        response.Content <- new StringContent(content, Encoding.UTF8, "application/json")
        response.StatusCode <- HttpStatusCode.OK
    
        response
        
    [<FunctionName("FaceMoji")>]
    let FaceMoji ([<HttpTrigger(AuthorizationLevel.Function, "post", Route="facemoji")>]req: HttpRequestMessage) =  
        async {   
            let! content = req.Content.ReadAsStringAsync()
                            |> Async.AwaitTask
            let photo = JsonConvert.DeserializeObject<Photo>(content)
         
            let bytes = Convert.FromBase64String(photo.PhotoBase64);

            let! faces = getFaces bytes
            return faces |> drawImage bytes
                         |> createResponse
        } |> Async.RunSynchronously