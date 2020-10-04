﻿using MixItUp.Base.Services.External;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Model.Actions
{
    public enum StreamingSoftwareTypeEnum
    {
        DefaultSetting,

        OBSStudio,
        XSplit,
        StreamlabsOBS,
    }

    public enum StreamingSoftwareActionTypeEnum
    {
        Scene,

        SourceVisibility,
        TextSource,
        WebBrowserSource,
        SourceDimensions,

        StartStopStream,

        SaveReplayBuffer,

        SceneCollection,
    }

    [DataContract]
    public class StreamingSoftwareSourceDimensionsModel
    {
        [DataMember]
        public int X { get; set; }
        [DataMember]
        public int Y { get; set; }
        [DataMember]
        public int Rotation { get; set; }
        [DataMember]
        public float XScale { get; set; }
        [DataMember]
        public float YScale { get; set; }

        public StreamingSoftwareSourceDimensionsModel(int x, int y, int rotation, float xScale, float yScale)
        {
            this.X = x;
            this.Y = y;
            this.Rotation = rotation;
            this.XScale = xScale;
            this.YScale = yScale;
        }

        public StreamingSoftwareSourceDimensionsModel() { }
    }

    [DataContract]
    public class StreamingSoftwareActionModel : ActionModelBase
    {
        private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

        protected override SemaphoreSlim AsyncSemaphore { get { return StreamingSoftwareActionModel.asyncSemaphore; } }

        public const string SourceTextFilesDirectoryName = "SourceTextFiles";

        public static async Task<StreamingSoftwareSourceDimensionsModel> GetSourceDimensions(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName)
        {
            StreamingSoftwareSourceDimensionsModel dimensions = null;

            if (softwareType == StreamingSoftwareTypeEnum.DefaultSetting)
            {
                // TODO
            }

            if (softwareType == StreamingSoftwareTypeEnum.OBSStudio)
            {
                if (ChannelSession.Services.OBSStudio.IsConnected || (await ChannelSession.Services.OBSStudio.Connect()).Success)
                {
                    // TODO
                    //dimensions = await ChannelSession.Services.OBSStudio.GetSourceDimensions(sceneName, sourceName);
                }
            }
            else if (softwareType == StreamingSoftwareTypeEnum.StreamlabsOBS)
            {
                if (ChannelSession.Services.StreamlabsOBS.IsConnected || (await ChannelSession.Services.StreamlabsOBS.Connect()).Success)
                {
                    // TODO
                    //dimensions = await ChannelSession.Services.StreamlabsOBS.GetSourceDimensions(sceneName, sourceName);
                }
            }

            return dimensions;
        }

        public static StreamingSoftwareActionModel CreateSceneAction(StreamingSoftwareTypeEnum softwareType, string sceneName)
        {
            StreamingSoftwareActionModel action = new StreamingSoftwareActionModel(softwareType, StreamingSoftwareActionTypeEnum.Scene);
            action.ItemName = sceneName;
            return action;
        }

        public static StreamingSoftwareActionModel CreateSourceVisibilityAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible)
        {
            StreamingSoftwareActionModel action = new StreamingSoftwareActionModel(softwareType, StreamingSoftwareActionTypeEnum.SourceVisibility);
            action.ParentName = sceneName;
            action.ItemName = sourceName;
            action.Visible = sourceVisible;
            return action;
        }

        public static StreamingSoftwareActionModel CreateTextSourceAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible, string sourceText, string sourceTextFilePath)
        {
            StreamingSoftwareActionModel action = StreamingSoftwareActionModel.CreateSourceVisibilityAction(softwareType, sceneName, sourceName, sourceVisible);
            action.ActionType = StreamingSoftwareActionTypeEnum.TextSource;
            action.SourceText = sourceText;
            action.SourceTextFilePath = sourceTextFilePath;
            return action;
        }

        public static StreamingSoftwareActionModel CreateWebBrowserSourceAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible, string sourceURL)
        {
            StreamingSoftwareActionModel action = StreamingSoftwareActionModel.CreateSourceVisibilityAction(softwareType, sceneName, sourceName, sourceVisible);
            action.ActionType = StreamingSoftwareActionTypeEnum.WebBrowserSource;
            action.SourceURL = sourceURL;
            if (softwareType == StreamingSoftwareTypeEnum.XSplit)
            {
                if (!File.Exists(action.SourceURL) && !action.SourceURL.Contains("://"))
                {
                    action.SourceURL = "http://" + action.SourceURL;
                }
            }
            return action;
        }

        public static StreamingSoftwareActionModel CreateSourceDimensionsAction(StreamingSoftwareTypeEnum softwareType, string sceneName, string sourceName, bool sourceVisible, StreamingSoftwareSourceDimensionsModel sourceDimensions)
        {
            StreamingSoftwareActionModel action = StreamingSoftwareActionModel.CreateSourceVisibilityAction(softwareType, sceneName, sourceName, sourceVisible);
            action.ActionType = StreamingSoftwareActionTypeEnum.SourceDimensions;
            action.SourceDimensions = sourceDimensions;
            return action;
        }

        public static StreamingSoftwareActionModel CreateStartStopStreamAction(StreamingSoftwareTypeEnum softwareType)
        {
            return new StreamingSoftwareActionModel(softwareType, StreamingSoftwareActionTypeEnum.StartStopStream);
        }

        public static StreamingSoftwareActionModel CreateSaveReplayBufferAction(StreamingSoftwareTypeEnum softwareType)
        {
            return new StreamingSoftwareActionModel(softwareType, StreamingSoftwareActionTypeEnum.SaveReplayBuffer);
        }

        public static StreamingSoftwareActionModel CreateSceneCollectionAction(StreamingSoftwareTypeEnum softwareType, string sceneCollectionName)
        {
            return new StreamingSoftwareActionModel(softwareType, StreamingSoftwareActionTypeEnum.SceneCollection)
            {
                ItemName = sceneCollectionName
            };
        }

        [DataMember]
        public StreamingSoftwareTypeEnum StreamingSoftwareType { get; set; }
        [DataMember]
        public StreamingSoftwareActionTypeEnum ActionType { get; set; }

        [DataMember]
        public string ItemName { get; set; }
        [DataMember]
        public string ParentName { get; set; }

        [DataMember]
        public bool Visible { get; set; }

        [DataMember]
        public string SourceText { get; set; }
        [DataMember]
        public string SourceTextFilePath { get; set; }

        [DataMember]
        public string SourceURL { get; set; }

        [DataMember]
        public StreamingSoftwareSourceDimensionsModel SourceDimensions { get; set; }

        public StreamingSoftwareActionModel(StreamingSoftwareTypeEnum softwareType, StreamingSoftwareActionTypeEnum actionType)
            : base(ActionTypeEnum.StreamingSoftware)
        {
            this.StreamingSoftwareType = softwareType;
            this.ActionType = actionType;
        }

        internal StreamingSoftwareActionModel(MixItUp.Base.Actions.StreamingSoftwareAction action)
            : base(ActionTypeEnum.StreamingSoftware)
        {
            this.StreamingSoftwareType = (StreamingSoftwareTypeEnum)(int)action.SoftwareType;
            this.ActionType = (StreamingSoftwareActionTypeEnum)(int)action.ActionType;
            if (this.ActionType == StreamingSoftwareActionTypeEnum.SceneCollection)
            {
                this.ItemName = action.SceneCollectionName;
            }
            else if (this.ActionType == StreamingSoftwareActionTypeEnum.Scene)
            {
                this.ItemName = action.SceneName;
            }
            else if (this.ActionType == StreamingSoftwareActionTypeEnum.SourceDimensions || this.ActionType == StreamingSoftwareActionTypeEnum.SourceVisibility ||
                this.ActionType == StreamingSoftwareActionTypeEnum.TextSource || this.ActionType == StreamingSoftwareActionTypeEnum.WebBrowserSource)
            {
                this.ItemName = action.SourceName;
                this.ParentName = action.SceneName;
            }
            this.Visible = action.SourceVisible;
            this.SourceText = action.SourceText;
            this.SourceTextFilePath = action.SourceTextFilePath;
            this.SourceURL = action.SourceURL;
            if (action.SourceDimensions != null)
            {
                this.SourceDimensions = new StreamingSoftwareSourceDimensionsModel(action.SourceDimensions.X, action.SourceDimensions.Y, action.SourceDimensions.Rotation, action.SourceDimensions.XScale, action.SourceDimensions.YScale);
            }
        }

        // TODO
        public StreamingSoftwareTypeEnum SelectedStreamingSoftware { get { return StreamingSoftwareTypeEnum.DefaultSetting; } }// (this.SoftwareType == StreamingSoftwareTypeEnum.DefaultSetting) ? ChannelSession.Settings.DefaultStreamingSoftware : this.SoftwareType; } }

        protected override async Task PerformInternal(UserViewModel user, StreamingPlatformTypeEnum platform, IEnumerable<string> arguments, Dictionary<string, string> specialIdentifiers)
        {
            IStreamingSoftwareService ssService = null;
            if (this.SelectedStreamingSoftware == StreamingSoftwareTypeEnum.OBSStudio)
            {
                ssService = ChannelSession.Services.OBSStudio;
            }
            else if (this.SelectedStreamingSoftware == StreamingSoftwareTypeEnum.XSplit)
            {
                ssService = ChannelSession.Services.XSplit;
            }
            else if (this.SelectedStreamingSoftware == StreamingSoftwareTypeEnum.StreamlabsOBS)
            {
                ssService = ChannelSession.Services.StreamlabsOBS;
            }

            if (ssService != null && ssService.IsEnabled)
            {
                Logger.Log(LogLevel.Debug, "Checking for Streaming Software connection");

                if (!ssService.IsConnected)
                {
                    Result result = await ssService.Connect();
                    if (!result.Success)
                    {
                        Logger.Log(LogLevel.Error, result.Message);
                        return;
                    }
                }

                Logger.Log(LogLevel.Debug, "Performing for Streaming Software connection");

                if (ssService.IsConnected)
                {
                    string name = null;
                    if (!string.IsNullOrEmpty(this.ItemName))
                    {
                        name = await this.ReplaceStringWithSpecialModifiers(this.ItemName, user, platform, arguments, specialIdentifiers);
                    }

                    string parentName = null;
                    if (!string.IsNullOrEmpty(this.ParentName))
                    {
                        parentName = await this.ReplaceStringWithSpecialModifiers(this.ParentName, user, platform, arguments, specialIdentifiers);
                    }

                    if (this.ActionType == StreamingSoftwareActionTypeEnum.StartStopStream)
                    {
                        await ssService.StartStopStream();
                    }
                    else if (this.ActionType == StreamingSoftwareActionTypeEnum.SaveReplayBuffer)
                    {
                        await ssService.SaveReplayBuffer();
                    }
                    else if (this.ActionType == StreamingSoftwareActionTypeEnum.Scene && !string.IsNullOrEmpty(name))
                    {
                        await ssService.ShowScene(name);
                    }
                    else if (!string.IsNullOrEmpty(name))
                    {
                        if (this.ActionType == StreamingSoftwareActionTypeEnum.WebBrowserSource && !string.IsNullOrEmpty(this.SourceURL))
                        {
                            await ssService.SetWebBrowserSourceURL(parentName, name, await this.ReplaceStringWithSpecialModifiers(this.SourceURL, user, platform, arguments, specialIdentifiers));
                        }
                        else if (this.ActionType == StreamingSoftwareActionTypeEnum.TextSource && !string.IsNullOrEmpty(this.SourceText) && !string.IsNullOrEmpty(this.SourceTextFilePath))
                        {
                            try
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(this.SourceTextFilePath)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(this.SourceTextFilePath));
                                }

                                using (StreamWriter writer = new StreamWriter(File.Open(this.SourceTextFilePath, FileMode.Create)))
                                {
                                    writer.Write(await this.ReplaceStringWithSpecialModifiers(this.SourceText, user, platform, arguments, specialIdentifiers));
                                    writer.Flush();
                                }
                            }
                            catch (Exception ex) { Logger.Log(ex); }
                        }
                        else if (this.ActionType == StreamingSoftwareActionTypeEnum.SourceDimensions && this.SourceDimensions != null)
                        {
                            // TODO
                            //await ssService.SetSourceDimensions(parentName, name, this.SourceDimensions);
                        }
                        await ssService.SetSourceVisibility(parentName, name, this.Visible);
                    }
                    else if (this.ActionType == StreamingSoftwareActionTypeEnum.SceneCollection && !string.IsNullOrEmpty(name))
                    {
                        await ssService.SetSceneCollection(name);
                    }
                }
            }
            else
            {
                Logger.Log(LogLevel.Error, "The Streaming Software selected is not enabled: " + this.SelectedStreamingSoftware);
            }
        }
    }
}