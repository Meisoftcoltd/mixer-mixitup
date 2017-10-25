﻿using Mixer.Base.Model.User;
using Mixer.Base.Util;
using MixItUp.Base.ViewModel.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MixItUp.Base.Actions
{
    public enum ActionTypeEnum
    {
        Chat,
        Currency,
        [Name("External Program")]
        ExternalProgram,
        Input,
        Overlay,
        Sound,
        Wait,
        [Name("OBS Studio")]
        OBSStudio,
        XSplit,
        Counter,
        [Name("Game Queue")]
        GameQueue,

        Custom = 99,
    }

    [DataContract]
    public abstract class ActionBase
    {
        [DataMember]
        public ActionTypeEnum Type { get; set; }

        public ActionBase() { }

        public ActionBase(ActionTypeEnum type)
        {
            this.Type = type;
        }

        public abstract Task Perform(UserViewModel user, IEnumerable<string> arguments);

        protected async Task<string> ReplaceStringWithSpecialModifiers(string str, UserViewModel user, IEnumerable<string> arguments)
        {
            if (user != null)
            {
                if (ChannelSession.ChatUsers.ContainsKey(user.ID))
                {
                    str = str.Replace("$useravatar", ChannelSession.ChatUsers[user.ID].AvatarLink);
                }
                else
                {
                    UserModel argUser = await ChannelSession.Connection.GetUser(user.UserName);
                    str = str.Replace("$useravatar", argUser.avatarUrl);
                }
                str = str.Replace("$userurl", "https://www.mixer.com/" + user.UserName);
                str = str.Replace("$user", user.UserName);

                if (ChannelSession.Settings.UserData.ContainsKey(user.ID))
                {
                    str = str.Replace("$usercurrency", ChannelSession.Settings.UserData[user.ID].CurrencyAmount.ToString());
                }
            }

            if (!string.IsNullOrEmpty(ChannelSession.Settings.CurrencyName))
            {
                str = str.Replace("$currencyname", ChannelSession.Settings.CurrencyName);
            }

            str = str.Replace("$date", DateTimeOffset.Now.ToString("g"));

            if (arguments != null)
            {
                for (int i = 0; i < arguments.Count(); i++)
                {
                    string username = arguments.ElementAt(i);
                    username = username.Replace("@", "");

                    UserModel argUser = await ChannelSession.Connection.GetUser(username);
                    if (argUser != null)
                    {
                        if (ChannelSession.Settings.UserData.ContainsKey(argUser.id))
                        {
                            str = str.Replace("$arg" + (i + 1) + "usercurrency", ChannelSession.Settings.UserData[argUser.id].CurrencyAmount.ToString());
                        }
                        else
                        {
                            str = str.Replace("$arg" + (i + 1) + "usercurrency", "0");
                        }                  
                        str = str.Replace("$arg" + (i + 1) + "useravatar", argUser.avatarUrl);
                        str = str.Replace("$arg" + (i + 1) + "userurl", "https://www.mixer.com/" + argUser.username);
                        str = str.Replace("$arg" + (i + 1) + "user", argUser.username);
                    }

                    str = str.Replace("$arg" + (i + 1), arguments.ElementAt(i));
                }
            }

            foreach (string counter in ChannelSession.Counters.Keys)
            {
                str = str.Replace("$" + counter, ChannelSession.Counters[counter].ToString());
            }

            return str;
        }
    }
}
