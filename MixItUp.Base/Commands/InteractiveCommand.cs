﻿using Mixer.Base.Model.Interactive;
using Mixer.Base.Util;
using MixItUp.Base.Actions;
using MixItUp.Base.Services;
using MixItUp.Base.ViewModel.Requirement;
using MixItUp.Base.ViewModel.User;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base.Commands
{
    public enum InteractiveButtonCommandTriggerType
    {
        [Name("Mouse Down")]
        MouseDown,
        [Name("Mouse Up")]
        MouseUp,
        [Name("Key Up")]
        KeyUp,
        [Name("Key Down")]
        KeyDown,
    }

    public enum InteractiveJoystickSetupType
    {
        [Name("Directional Arrows")]
        DirectionalArrows,
        WASD,
        [Name("Mouse Movement")]
        MouseMovement,
        [Name("Map To Individual Keys")]
        MapToIndividualKeys,
    }

    public class InteractiveCommand : PermissionsCommandBase
    {
        private static SemaphoreSlim interactiveCommandPerformSemaphore = new SemaphoreSlim(1);

        [JsonProperty]
        public uint GameID { get; set; }

        [JsonProperty]
        public string SceneID { get; set; }

        [JsonProperty]
        public InteractiveControlModel Control { get; set; }

        [JsonProperty]
        [Obsolete]
        public int IndividualCooldown { get; set; }

        [JsonProperty]
        [Obsolete]
        public string CooldownGroup { get; set; }

        protected override SemaphoreSlim AsyncSemaphore { get { return InteractiveCommand.interactiveCommandPerformSemaphore; } }

        public InteractiveCommand() { }

        protected InteractiveCommand(InteractiveGameListingModel game, InteractiveSceneModel scene, InteractiveControlModel control, string command, RequirementViewModel requirements)
            : base(control.controlID, CommandTypeEnum.Interactive, command, requirements)
        {
            this.GameID = game.id;
            this.SceneID = scene.sceneID;
            this.Control = control;
        }

        [JsonIgnore]
        public virtual string EventTypeString { get { return string.Empty; } }

        public void UpdateWithLatestControl(InteractiveControlModel control) { this.Control = control; }
    }

    public class InteractiveButtonCommand : InteractiveCommand
    {
        public const string BasicCommandCooldownGroup = "All Buttons";

        [JsonProperty]
        public InteractiveButtonCommandTriggerType Trigger { get; set; }

        public InteractiveButtonCommand() { }

        public InteractiveButtonCommand(InteractiveGameListingModel game, InteractiveSceneModel scene, InteractiveButtonControlModel control, InteractiveButtonCommandTriggerType eventType, RequirementViewModel requirements)
            : base(game, scene, control, EnumHelper.GetEnumName(eventType), requirements)
        {
            this.Trigger = eventType;
        }

        [JsonIgnore]
        public InteractiveButtonControlModel Button { get { return (InteractiveButtonControlModel)this.Control; } }

        [JsonIgnore]
        public int CooldownAmount
        {
            get
            {
                if (this.Requirements.Cooldown != null)
                {
                    return this.Requirements.Cooldown.CooldownAmount;
                }
                return 0;
            }
        }

        [JsonIgnore]
        public string CooldownGroupName
        {
            get
            {
                if (this.Requirements.Cooldown != null)
                {
                    return this.Requirements.Cooldown.GroupName;
                }
                return string.Empty;
            }
        }

        [JsonIgnore]
        public override string EventTypeString { get { return this.Trigger.ToString().ToLower(); } }

        public long GetCooldownTimestamp()
        {
            if (this.Requirements.Cooldown != null && (this.Requirements.Cooldown.Type == CooldownTypeEnum.Global || this.Requirements.Cooldown.Type == CooldownTypeEnum.Group))
            {
                return DateTimeHelper.DateTimeOffsetToUnixTimestamp(DateTimeOffset.Now.AddSeconds(this.CooldownAmount));
            }
            return DateTimeHelper.DateTimeOffsetToUnixTimestamp(DateTimeOffset.Now);
        }
    }

    public class InteractiveJoystickCommand : InteractiveCommand
    {
        private class InteractiveJoystickAction : ActionBase
        {
            private static SemaphoreSlim asyncSemaphore = new SemaphoreSlim(1);

            protected override SemaphoreSlim AsyncSemaphore { get { return InteractiveJoystickAction.asyncSemaphore; } }

            private InteractiveJoystickCommand command;

            public InteractiveJoystickAction(InteractiveJoystickCommand command) { this.command = command; }

            protected override Task PerformInternal(UserViewModel user, IEnumerable<string> arguments)
            {
                if (double.TryParse(arguments.ElementAt(0), out double x) && double.TryParse(arguments.ElementAt(1), out double y))
                {
                    if (this.command.SetupType == InteractiveJoystickSetupType.MouseMovement)
                    {
                        if (!(x > command.DeadZone || x < -command.DeadZone)) { x = 0.0; }
                        if (!(y > command.DeadZone || y < -command.DeadZone)) { y = 0.0; }

                        ChannelSession.Services.InputService.MoveMouse((int)(x * command.MouseMovementMultiplier), (int)(y * command.MouseMovementMultiplier));
                    }
                    else
                    {
                        List<InputKeyEnum?> keysToUse = new List<InputKeyEnum?>();
                        if (this.command.SetupType == InteractiveJoystickSetupType.DirectionalArrows)
                        {
                            keysToUse.Add(InputKeyEnum.Up);
                            keysToUse.Add(InputKeyEnum.Right);
                            keysToUse.Add(InputKeyEnum.Down);
                            keysToUse.Add(InputKeyEnum.Left);
                        }
                        else if (this.command.SetupType == InteractiveJoystickSetupType.WASD)
                        {
                            keysToUse.Add(InputKeyEnum.W);
                            keysToUse.Add(InputKeyEnum.A);
                            keysToUse.Add(InputKeyEnum.S);
                            keysToUse.Add(InputKeyEnum.D);
                        }
                        else
                        {
                            keysToUse = this.command.MappedKeys;
                        }

                        if (keysToUse[0] != null)
                        {
                            if (y < -command.DeadZone)
                            {
                                ChannelSession.Services.InputService.KeyDown(keysToUse[0].GetValueOrDefault());
                            }
                            else
                            {
                                ChannelSession.Services.InputService.KeyUp(keysToUse[0].GetValueOrDefault());
                            }
                        }

                        if (keysToUse[2] != null)
                        {
                            if (y > command.DeadZone)
                            {
                                ChannelSession.Services.InputService.KeyDown(keysToUse[2].GetValueOrDefault());
                            }
                            else
                            {
                                ChannelSession.Services.InputService.KeyUp(keysToUse[2].GetValueOrDefault());
                            }
                        }

                        if (keysToUse[1] != null)
                        {
                            if (x < -command.DeadZone)
                            {
                                ChannelSession.Services.InputService.KeyDown(keysToUse[1].GetValueOrDefault());
                            }
                            else
                            {
                                ChannelSession.Services.InputService.KeyUp(keysToUse[1].GetValueOrDefault());
                            }
                        }

                        if (keysToUse[3] != null)
                        {
                            if (x > command.DeadZone)
                            {
                                ChannelSession.Services.InputService.KeyDown(keysToUse[3].GetValueOrDefault());
                            }
                            else
                            {
                                ChannelSession.Services.InputService.KeyUp(keysToUse[3].GetValueOrDefault());
                            }
                        }
                    }
                }
                return Task.FromResult(0);
            }
        }

        [JsonProperty]
        public InteractiveJoystickSetupType SetupType { get; set; }

        [JsonProperty]
        public double DeadZone { get; set; }

        [JsonProperty]
        public List<InputKeyEnum?> MappedKeys { get; set; }

        [JsonProperty]
        public double MouseMovementMultiplier { get; set; }

        public InteractiveJoystickCommand()
        {
            this.MappedKeys = new List<InputKeyEnum?>();
        }

        public InteractiveJoystickCommand(InteractiveGameListingModel game, InteractiveSceneModel scene, InteractiveJoystickControlModel control, RequirementViewModel requirements)
            : base(game, scene, control, string.Empty, requirements)
        {
            this.MappedKeys = new List<InputKeyEnum?>();
        }

        [JsonIgnore]
        public InteractiveJoystickControlModel Joystick { get { return (InteractiveJoystickControlModel)this.Control; } }

        [JsonIgnore]
        public override string EventTypeString { get { return "move"; } }

        public void InitializeAction()
        {
            this.Actions.Clear();
            this.Actions.Add(new InteractiveJoystickAction(this));
        }

        protected override async Task PerformInternal(UserViewModel user, IEnumerable<string> arguments, CancellationToken token)
        {
            await base.PerformInternal(user, arguments, token);
        }

        [OnDeserialized]
        internal void OnDeserialized()
        {
            this.InitializeAction();
        }
    }
}
