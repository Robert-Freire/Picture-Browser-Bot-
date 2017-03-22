using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Bot_Application1.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private const string actionLoadFile = "load file";
        private const string actionLoadFolder = "load folder";

        private const string SubscriptionKey = "83d8c47c6f1845f9a8fc174e7024952b";

        private enum actions
        {
            loadFile,
            loadFolder,
            notdefined
        }

        private struct botAction
        {
            public actions action;
            public string text;
        }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            var action = getAction(activity);
            
            if (action.action == actions.loadFile)
            {
                await loadUrl(context, action.text);
                return;
            }

            if (action.action == actions.loadFolder)
            {
                var results = await loadFolder(context, action.text);
                var resultsGrouped = results.GroupBy(AnalysisResult => AnalysisResult.Categories[0].Name)
                    .Select(group => new
                    {
                        category = group.Key,
                        count = group.Count()
                    }).OrderBy(x=> x.category);
                await context.PostAsync($"Of witch");
                foreach (var res in resultsGrouped)
                {
                    await context.PostAsync($"{res.count} of category {res.category }");
                }

                return;
            }

            // calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;

            // return our reply to the user
            await context.PostAsync($"You sent {activity.Text} which was {length} characters");

            context.Wait(MessageReceivedAsync);
        }

        private botAction getAction(Activity activity)
        {
            if (activity.Text.ToUpper().StartsWith(actionLoadFile.ToUpper()))
            {
                return new botAction { action = actions.loadFile, text = activity.Text.Substring(actionLoadFile.Length) };
            }

            if (activity.Text.ToUpper().StartsWith(actionLoadFolder.ToUpper()))
            {
                return new botAction { action = actions.loadFolder, text = activity.Text.Substring(actionLoadFolder.Length) };
            }

            return new botAction { action = actions.notdefined, text = activity.Text };
        }

        private async Task loadUrl(IDialogContext context, string file)
        {
            var SubscriptionKey = "83d8c47c6f1845f9a8fc174e7024952b";


            showFile(context, file);
            //
            // Create Project Oxford Vision API Service client
            //
            VisionServiceClient VisionServiceClient = new VisionServiceClient(SubscriptionKey);
            //Log("VisionServiceClient is created");

            //
            // Analyze the url for all visual features
            //
            //Log("Calling VisionServiceClient.AnalyzeImageAsync()...");
            VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
            AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(file, visualFeatures);
            
            await context.PostAsync($"You sent a image of type { analysisResult.Categories[0].Name}");
            
         //   return analysisResult;
        }

        private async Task<List<AnalysisResult>> loadFolder(IDialogContext context, string folder)
        {
            string[] fileEntries = Directory.GetFiles(folder);
            var results = new List<AnalysisResult>();

            //Parallel.ForEach (fileEntries, async (currentFile) =>
            //{
            //    results.Add(await loadFileAsync(currentFile));
            //});

            await context.PostAsync($"You sent {fileEntries.Length} files.");
            foreach (var currentFile in fileEntries)
            {
                results.Add(await loadFileAsync(currentFile));
            }


            return results;

            //   return analysisResult;
        }

        private async Task<AnalysisResult> loadFileAsync(string file)
        {
            //
            // Create Project Oxford Vision API Service client
            //
            VisionServiceClient VisionServiceClient = new VisionServiceClient(SubscriptionKey);
//            Log("VisionServiceClient is created");

            using (Stream imageFileStream = File.OpenRead(file))
            {
                //
                // Analyze the image for all visual features
                //
//                Log("Calling VisionServiceClient.AnalyzeImageAsync()...");
                VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories, VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType, VisualFeature.Tags };
                AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);
                return analysisResult;
            }
        }

        private void showFile(IDialogContext context, string file)
        {
            var message = context.MakeMessage();
            message.Attachments.Add(new Attachment()
            {
                ContentUrl = file,
                ContentType = "image/png",
                Name = ""
            });

            context.PostAsync(message);
        }

    }
}