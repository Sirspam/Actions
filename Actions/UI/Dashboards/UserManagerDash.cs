﻿using HMUI;
using System;
using Zenject;
using Tweening;
using System.Linq;
using UnityEngine;
using Actions.Twitch;
using Actions.Dashboard;
using Actions.Components;
using System.ComponentModel;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;

namespace Actions.UI.Dashboards
{
    [ViewDefinition("Actions.Views.user-manager-dash.bsml")]
    [HotReload(RelativePathToLayout = @"..\..\Views\user-manager-dash.bsml")]
    internal class UserManagerDash : FloatingViewController<UserManagerDash>, IInitializable, IDisposable
    {
        [Inject]
        private readonly TweeningManager _tweeningManager = null!;

        [Inject]
        private readonly PlatformManager _platformManager = null!;

        [UIValue("user-hosts")]
        protected readonly List<object> userHosts = new List<object>();

        [UIParams]
        protected readonly BSMLParserParams parserParams = null!;

        [UIComponent("user-container")]
        protected readonly RectTransform userContainer = null!;

        [UIValue("timeout-value")]
        protected ManagementAction SelectedAction { get; set; }

        [UIComponent("name-text")]
        protected readonly CurvedTextMeshPro nameText = null!;

        protected CanvasGroup userContainerCanvas = null!;
        private IActionUser? _lastClickedUser;
        private bool opened = false;

        public void Initialize()
        {
            gameObject.SetActive(true);
            _platformManager.ChannelActivity += ActivityReceived;
        }

        private void ActivityReceived(IActionUser user)
        {
            // progen shift
            var firstHost = (userHosts[0] as UserHost)!;

            // wont change anything
            if (firstHost.User == user) return;

            var hosts = userHosts.Cast<UserHost>().ToArray();

            var existingHost = hosts.FirstOrDefault(uh => uh.User == user);
            int indexOrLast = existingHost is null ? hosts.Count() - 1 : hosts.IndexOf(existingHost);
            for (int i = indexOrLast; i > 0; i--)
            {
                var current = hosts.ElementAt(i);
                var newer = hosts.ElementAt(i - 1);
                current.User = newer.User;
            }
            firstHost.User = user;
        }

        public void Dispose()
        {
            _platformManager.ChannelActivity -= ActivityReceived;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            userHosts.Add(new UserHost(UserClicked));
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            nameText.fontSizeMin = 4.5f;
            nameText.fontSizeMax = 7.5f;
            nameText.enableAutoSizing = true;

            userContainerCanvas = userContainer.gameObject.AddComponent<CanvasGroup>();
            userContainerCanvas.alpha = 0f;
        }

        [UIAction("toggle")]
        protected void Clicked()
        {
            _tweeningManager.KillAllTweens(this);
            var currentAlpha = userContainerCanvas.alpha;
            if (opened)
            {
                _tweeningManager.AddTween(new FloatTween(currentAlpha, 0f, UpdateCanvasAlpha, 0.5f, EaseType.InOutQuad), this);
            }
            else
            {
                _tweeningManager.AddTween(new FloatTween(currentAlpha, 1f, UpdateCanvasAlpha, 0.5f, EaseType.InOutQuad), this);
            }
            opened = !opened;
        }

        private void UpdateCanvasAlpha(float val)
        {
            userContainerCanvas.alpha = val;
        }

        private void UserClicked(IActionUser user)
        {
            _lastClickedUser = user;
            parserParams.EmitEvent("show-modal");
            nameText.text = _lastClickedUser.Name;
            nameText.fontSizeMax = 10.0f;
            nameText.fontSizeMin = 4.5f;
        }

        [UIAction("format-timeout")]
        protected string FormatTimeout(ManagementAction action)
        {
            return (action switch
            {
                ManagementAction.Seconds1 => "1 Second",
                ManagementAction.Seconds30 => "30 Seconds",
                ManagementAction.Minutes1 => "1 Minute",
                ManagementAction.Minutes5 => "5 Minutes",
                ManagementAction.Minutes10 => "10 Minutes",
                ManagementAction.Minutes30 => "30 Minutes",
                ManagementAction.Hours1 => "1 Hour",
                ManagementAction.Hours4 => "4 Hours",
                ManagementAction.Hours12 => "12 Hours",
                ManagementAction.Days1 => "1 Day",
                ManagementAction.Days2 => "2 Days",
                ManagementAction.Weeks1 => "1 Week",
                ManagementAction.Weeks2 => "2 Weeks",
                ManagementAction.Forever => "Forever (Ban)",
                _ => throw new NotImplementedException()
            }).ToString();
        }

        [UIAction("timeout")]
        protected void Timeout()
        {
            if (_lastClickedUser is null || !(_lastClickedUser is TwitchActionUser))
                return;

            float? timeoutDuration = SelectedAction switch
            {
                ManagementAction.Seconds1 => 1,
                ManagementAction.Seconds30 => 30,
                ManagementAction.Minutes1 => 60,
                ManagementAction.Minutes5 => 300,
                ManagementAction.Minutes10 => 600,
                ManagementAction.Minutes30 => 1800,
                ManagementAction.Hours1 => 3600,
                ManagementAction.Hours4 => 14400,
                ManagementAction.Hours12 => 43200,
                ManagementAction.Days1 => 86400,
                ManagementAction.Days2 => 172800,
                ManagementAction.Weeks1 => 604800,
                ManagementAction.Weeks2 => 1209600,
                ManagementAction.Forever => null,
                _ => throw new NotImplementedException()
            };


            _lastClickedUser.Ban(timeoutDuration);
        }

        // TODO: Only update image if the menu is active/just turned on
        public class UserHost : INotifyPropertyChanged
        {
            [UIComponent("user-image")]
            protected readonly ImageView _userImage = null!;

            [UIValue("has-content")]
            protected bool HasContent => !(User is null);
            public event PropertyChangedEventHandler? PropertyChanged;

            private Action<IActionUser>? _clickedCallback;
            private IActionUser? _user;
            public IActionUser? User
            {
                get => _user;
                set
                {
                    _user = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasContent)));
                    if (_user != null && _user.ProfilePictureURL != null)
                    {
                        _userImage.SetImage(_user.ProfilePictureURL);
                    }
                }
            }

            public UserHost(Action<IActionUser>? clickedCallback = null)
            {
                _clickedCallback = clickedCallback;
            }

            [UIAction("clicked")]
            protected void Clicked()
            {
                _clickedCallback?.Invoke(_user!);
            }
        }
    }
}