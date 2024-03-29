﻿using Microsoft.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3
{
    public class WindowMessageReceivedEventArgs
    {
        public WindowMessageReceivedEventArgs() { }

        public WindowMessageReceivedEventArgs(WindowMessageReceivedEventArgs args) : this()
        {
            WindowId = args.WindowId;
            MessageId = args.MessageId;
            WParam = args.WParam;
            LParam = args.LParam;
            LResult = args.LResult;
        }

        public WindowId WindowId { get; internal set; }

        public uint MessageId { get; internal set; }

        public nuint WParam { get; internal set; }

        public nint LParam { get; internal set; }

        public bool Handled { get; set; }

        public nint LResult { get; set; }

#if DEBUG
        public override string ToString()
        {
            return GetMessageName(MessageId);
        }
#endif

#if DEBUG

        private static IReadOnlyDictionary<uint, string>? messageNameMap;
        private static object messageMapLocker = new object();

        internal static string GetMessageName(uint messageId)
        {
            if (messageNameMap == null)
            {
                lock (messageMapLocker)
                {
                    if (messageNameMap == null)
                    {
                        messageNameMap = typeof(Windows.Win32.PInvoke).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                            .Where(c => c.Name.StartsWith("WM_") && c.FieldType == typeof(uint))
                            .Select(c => (id: (uint)c.GetValue(null)!, name: c.Name))
                            .GroupBy(c => c.id)
                            .ToDictionary(c => c.Key, c => c.FirstOrDefault().name);
                    }
                }
            }

            if (messageNameMap.TryGetValue(messageId, out var name)) return name;

            return "UNKNOWN";
        }

#else
        internal static string GetMessageName(uint messageId)
        {
            return "UNKNOWN";
        }
#endif

    }

    public delegate void WindowMessageReceivedEventHandler(WindowManager sender, WindowMessageReceivedEventArgs e);

    internal static class WindowMessageReceivedEventArgsPool
    {
        private static ConcurrentQueue<WindowMessageReceivedEventArgs> items = new ConcurrentQueue<WindowMessageReceivedEventArgs>();
        private static WindowMessageReceivedEventArgs? fastItem;
        private static int itemsCount = 0;
        private static readonly int maxCapacity = Environment.ProcessorCount * 2 - 1;

        public static WindowMessageReceivedEventArgs Get()
        {
            var item = fastItem;

            if (item == null || Interlocked.CompareExchange(ref fastItem, null, item) != item)
            {
                if (items.TryDequeue(out item))
                {
                    Interlocked.Increment(ref itemsCount);

                    item.WindowId = default;
                    item.MessageId = default;
                    item.WParam = default;
                    item.LParam = default;
                    item.LResult = 0;
                    item.Handled = false;

                    return item;
                }

                return new WindowMessageReceivedEventArgs();
            }

            item.WindowId = default;
            item.MessageId = default;
            item.WParam = default;
            item.LParam = default;
            item.LResult = 0;
            item.Handled = false;

            return item!;
        }

        public static bool Return(WindowMessageReceivedEventArgs obj)
        {
            if (fastItem != null || Interlocked.CompareExchange(ref fastItem, obj, null) != null)
            {
                if (Interlocked.Increment(ref itemsCount) <= maxCapacity)
                {
                    items.Enqueue(obj);
                    return true;
                }

                Interlocked.Decrement(ref itemsCount);
                return false;
            }

            return true;
        }
    }
}
