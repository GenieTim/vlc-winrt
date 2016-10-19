﻿/**********************************************************************
 * VLC for WinRT
 **********************************************************************
 * Copyright © 2013-2014 VideoLAN and Authors
 *
 * Licensed under GPLv2+ and MPLv2
 * Refer to COPYING file of the official project for license
 **********************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VLC.Model;
using VLC.Model.FileExplorer;
using VLC.Utils;
using VLC.ViewModels;
using VLC.ViewModels.Others.VlcExplorer;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.UI.Core;

namespace VLC.Services.RunTime
{
    public class ExternalDeviceService : IDisposable
    {
        private DeviceWatcher _deviceWatcher;

        public void startWatcher()
        {
            _deviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
            _deviceWatcher.Added += DeviceAdded;
            _deviceWatcher.Removed += DeviceRemoved;
            _deviceWatcher.Start();
        }

        public void Dispose()
        {
            if (_deviceWatcher != null)
            {
                _deviceWatcher.Stop();
                _deviceWatcher.Added -= DeviceAdded;
                _deviceWatcher.Removed -= DeviceRemoved;
                _deviceWatcher = null;
            }
        }

        public delegate Task ExternalDeviceAddedEvent(DeviceWatcher sender, string Id);
        public delegate Task ExternalDeviceRemovedEvent(DeviceWatcher sender, string Id);
        public delegate Task MustIndexExternalDeviceEvent();
        public delegate Task MustUnindexExternalDeviceEvent();

        public ExternalDeviceAddedEvent ExternalDeviceAdded;
        public ExternalDeviceRemovedEvent ExternalDeviceRemoved;
        public MustIndexExternalDeviceEvent MustIndexExternalDevice;
        public MustUnindexExternalDeviceEvent MustUnindexExternalDevice;

        public async Task<IEnumerable<string>> GetExternalDeviceIds()
        {
            DeviceInformationCollection devices =
                await DeviceInformation.FindAllAsync(DeviceClass.PortableStorageDevice);

            return devices.Select(d => d.Id);
        }

        private async void DeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            switch (Locator.SettingsVM.ExternalDeviceMode)
            {
                case ExternalDeviceMode.AskMe:
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal,
                        () => Locator.NavigationService.Go(VLCPage.ExternalStorageInclude));
                    break;
                case ExternalDeviceMode.IndexMedias:
                    await AskExternalDeviceIndexing();
                    break;
                case ExternalDeviceMode.SelectMedias:
                    await DispatchHelper.InvokeAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await AskContentToCopy();
                    });
                    break;
                case ExternalDeviceMode.DoNothing:
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (ExternalDeviceAdded != null)
                await ExternalDeviceAdded(sender, args.Id);
        }

        public async Task AskExternalDeviceIndexing()
        {
            if (MustIndexExternalDevice != null)
                await MustIndexExternalDevice();
        }

        public async Task AskContentToCopy()
        {
            // Display the folder of the first external storage device detected.
            Locator.MainVM.CurrentPanel = Locator.MainVM.Panels.FirstOrDefault(x => x.Target == VLCPage.MainPageFileExplorer);

            var devices = KnownFolders.RemovableDevices;
            IReadOnlyList<StorageFolder> rootFolders = await devices.GetFoldersAsync();

            var rootFolder = rootFolders.First();
            if (rootFolder == null)
                return;

            var storageItem = new VLCStorageFolder(rootFolder);
            Locator.FileExplorerVM.CurrentStorageVM = new LocalFileExplorerViewModel(
                rootFolder, RootFolderType.ExternalDevice);
            await Locator.FileExplorerVM.CurrentStorageVM.GetFiles();
        }

        private async void DeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            if (ExternalDeviceRemoved != null)
                await ExternalDeviceRemoved(sender, args.Id);

            if (MustUnindexExternalDevice != null)
                await MustUnindexExternalDevice();
        }
    }
}
