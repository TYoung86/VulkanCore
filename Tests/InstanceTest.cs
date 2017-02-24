﻿using System;
using System.Collections.Generic;
using VulkanCore.Ext;
using VulkanCore.Tests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace VulkanCore.Tests
{
    public unsafe class InstanceTest : HandleTestBase
    {
        [Fact]
        public void Constructor_Succeeds()
        {
            using (new Instance()) { }
            using (var instance = new Instance(allocator: CustomAllocator))
            {
                Assert.Equal(CustomAllocator, instance.Allocator);
            }
        }

        [Fact]
        public void Constructor_ApplicationInfo_Succeeds()
        {
            var createInfo1 = new InstanceCreateInfo(new ApplicationInfo());
            var createInfo2 = new InstanceCreateInfo(new ApplicationInfo("app name", 1, "engine name", 2));
            using (new Instance(createInfo1)) { }
            using (new Instance(createInfo2)) { }
        }

        [Fact]
        public void Constructor_EnabledLayerAndExtension_Succeeds()
        {
            var createInfo = new InstanceCreateInfo(
                enabledLayerNames: new[] { Constant.InstanceLayer.LunarGStandardValidation },
                enabledExtensionNames: new[] { Constant.InstanceExtension.ExtDebugReport });

            using (new Instance(createInfo)) { }
        }

        [Fact]
        public void DisposeTwice_Succeeds()
        {
            var instance = new Instance();
            instance.Dispose();
            instance.Dispose();
        }

        [Fact]
        public void CreateDebugReportCallbackExt_Succeeds()
        {
            var createInfo = new InstanceCreateInfo(
                enabledLayerNames: new[] { Constant.InstanceLayer.LunarGStandardValidation },
                enabledExtensionNames: new[] { Constant.InstanceExtension.ExtDebugReport });

            using (var instance = new Instance(createInfo))
            {
                var callbackArgs = new List<DebugReportCallbackInfo>();
                int userData = 1;
                IntPtr userDataHandle = new IntPtr(&userData);
                var debugReportCallbackCreateInfo = new DebugReportCallbackCreateInfoExt(
                    DebugReportFlagsExt.All,
                    args =>
                    {
                        callbackArgs.Add(args);
                        return false;
                    },
                    userDataHandle);

                // Registering the callback should generate DEBUG messages.
                using (instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo)) { }
                using (instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo, CustomAllocator)) { }

                Assert.True(callbackArgs.Count > 0);
                Assert.Equal(1, *(int*)callbackArgs[0].UserData);
            }
        }

        [Fact]
        public void EnumeratePhysicalDevices_ReturnsAtLeastOneDevice()
        {
            PhysicalDevice[] physicalDevices = Instance.EnumeratePhysicalDevices();
            Assert.True(physicalDevices.Length > 0);
            Assert.Equal(Instance, physicalDevices[0].Parent);
        }

        [Fact]
        public void GetProcAddr_ReturnsValidHandleForExistingCommand()
        {
            IntPtr address = Instance.GetProcAddr("vkCreateDebugReportCallbackEXT");
            Assert.NotEqual(IntPtr.Zero, address);
        }

        [Fact]
        public void GetProcAddr_ReturnsNullHandleForMissingCommand()
        {
            IntPtr address = Instance.GetProcAddr("does not exist");
            Assert.Equal(IntPtr.Zero, address);
        }

        [Fact]
        public void GetProcAddr_ThrowsArgumentNullForNull()
        {
            Assert.Throws<ArgumentNullException>(() => Instance.GetProcAddr(null));
        }

        [Fact]
        public void GetProc_ThrowsArgumentNullForNull()
        {
            Assert.Throws<ArgumentNullException>(() => Instance.GetProc<EventHandler>(null));
        }

        [Fact]
        public void GetProc_ReturnsNullForMissingCommand()
        {
            Assert.Null(Instance.GetProc<EventHandler>("does not exist"));
        }

        [Fact]
        public void GetProc_ReturnsValidDelegate()
        {
            var commandDelegate = Instance.GetProc<CreateDebugReportCallbackExt>("vkCreateDebugReportCallbackEXT");
            Assert.NotNull(commandDelegate);
        }

        [Fact]
        public void EnumerateExtensionProperties_SucceedsWithoutLayerName()
        {
            ExtensionProperties[] properties = Instance.EnumerateExtensionProperties();
            Assert.True(properties.Length > 0);
        }

        [Fact]
        public void EnumerateExtensionProperties_SucceedsForLayerName()
        {
            ExtensionProperties[] properties = Instance.EnumerateExtensionProperties(
                Constant.InstanceLayer.LunarGStandardValidation);
            Assert.True(properties.Length > 0);

            ExtensionProperties firstProperty = properties[0];
            Assert.StartsWith(firstProperty.ExtensionName, properties[0].ToString());
        }

        [Fact]
        private void EnumerateLayerProperties_Succeeds()
        {
            LayerProperties[] properties = Instance.EnumerateLayerProperties();
            Assert.True(properties.Length > 0);

            LayerProperties firstProperty = properties[0];
            Assert.StartsWith(firstProperty.LayerName, properties[0].ToString());
        }

        [Fact]
        public void DebugReportMessageExt_Succeeds()
        {
            const string message = "message õäöü";
            const DebugReportObjectTypeExt objectType = DebugReportObjectTypeExt.DebugReport;
            const long @object = long.MaxValue;
            var location = new IntPtr(int.MaxValue);
            const int messageCode = 1;
            const string layerPrefix = "prefix õäöü";

            bool visitedCallback = false;

            var instanceCreateInfo = new InstanceCreateInfo(
                enabledExtensionNames: new[] { Constant.InstanceExtension.ExtDebugReport });
            using (var instance = new Instance(instanceCreateInfo))
            {
                var debugReportCallbackCreateInfo = new DebugReportCallbackCreateInfoExt(
                    DebugReportFlagsExt.Error,
                    args =>
                    {
                        Assert.Equal(objectType, args.ObjectType);
                        Assert.Equal(@object, args.Object);
                        Assert.Equal(location, args.Location);
                        Assert.Equal(messageCode, args.MessageCode);
                        Assert.Equal(layerPrefix, args.LayerPrefix);
                        Assert.Equal(message, args.Message);
                        visitedCallback = true;
                        return false;
                    });
                using (instance.CreateDebugReportCallbackExt(debugReportCallbackCreateInfo))
                {
                    instance.DebugReportMessageExt(DebugReportFlagsExt.Error, message, objectType,
                        @object, location, messageCode, layerPrefix);
                }
            }

            Assert.True(visitedCallback);
        }

        public InstanceTest(DefaultHandles defaults, ITestOutputHelper output) : base(defaults, output) { }

        private delegate Result CreateDebugReportCallbackExt(IntPtr p1, IntPtr p2, IntPtr p3, IntPtr p4);
    }
}
