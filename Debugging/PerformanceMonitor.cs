﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImGuiNET;

namespace SimpleTweaksPlugin.Debugging; 

public class PerformanceMonitor : DebugHelper {

    public class PerformanceLogger : IDisposable {
        private string? runKey;
        public PerformanceLogger(string key = null, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFileName = null) {
            var k = key;
            if (k == null && (callerFileName == null || callerMemberName == null)) return;
            k ??= $"{callerFileName}::{callerMemberName}";
            if (!Logs.ContainsKey(k)) Logs.Add(k, new PerformanceLog());
            Logs[k].Begin();
            runKey = k;
        }
        
        public void Dispose() {
            if (runKey == null) return;
            if (!Logs.ContainsKey(runKey)) return;
            Logs[runKey].End();
        }
    }

    public static PerformanceLogger Run(string key = null, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFileName = null) {
        var k = key;
        if (k == null && (callerFileName == null || callerMemberName == null)) return new PerformanceLogger();
        k ??= $"{callerFileName}::{callerMemberName}";
        return new PerformanceLogger(k);
    }
    
    public static bool DoFrameworkMonitor = false;

    private enum DisplayType {
        Ticks,
        Milliseconds,
    }

    private static DisplayType _displayType = DisplayType.Ticks;

    private void DisplayValue(long ticks) {
        var text = _displayType switch {
            DisplayType.Ticks => $"{ticks}",
            DisplayType.Milliseconds => $"{ticks / (float)TimeSpan.TicksPerMillisecond : 0.000}ms",
            _ => $"{ticks}"
        };
        
        ImGui.Text($"{text}");
    }
    
    public override void Draw() {
        ImGui.Separator();
        ImGui.Checkbox("Log Framework Events", ref DoFrameworkMonitor);
        ImGui.SameLine();

        if (ImGui.BeginCombo("Display Type", $"{_displayType}")) {
            foreach (var e in Enum.GetValues<DisplayType>()) {
                if (ImGui.Selectable($"{e}", _displayType == e)) _displayType = e;
            }
            ImGui.EndCombo();
        }

        Begin("PerformanceMonitor.Draw");

        if (ImGui.SmallButton("Reset All")) {
            ClearAll();
        }

        if (ImGui.BeginTable("performanceTable", 7, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollX)) {
            ImGui.TableSetupColumn("Reset");
            ImGui.TableSetupColumn("Key");
            ImGui.TableSetupColumn("Last Check");
            ImGui.TableSetupColumn("Average");
            ImGui.TableSetupColumn("Maximum");
            ImGui.TableSetupColumn("");
            ImGui.TableSetupColumn("Average/Sec");
                
            ImGui.TableHeadersRow();
            ImGui.TableNextColumn();

            foreach (var log in Logs) {
                if (ImGui.SmallButton($"Reset##{log.Key}")) log.Value.Clear();
                ImGui.TableNextColumn();
                ImGui.Text($"{log.Key}");
                ImGui.TableNextColumn();
                DisplayValue(log.Value.Last);
                ImGui.TableNextColumn();
                DisplayValue(log.Value.Average);
                ImGui.TableNextColumn();
                DisplayValue(log.Value.Max);
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
                ImGui.Text($"{log.Value.AveragePerSecond}");
                    
                ImGui.TableNextColumn();
            }
                
                
                
            ImGui.EndTable();
        }
            
        End("PerformanceMonitor.Draw");
    }

    public override string Name => "Performance";
        
    private class PerformanceLog {
        private Stopwatch stopwatch = new();
        private Stopwatch started = new();
        private Stopwatch total = new();

        public long Last { get; private set; } = -1;
        public long Max { get; private set; } = -1;

        public long Average { get; private set; } = -1;

        public long Count { get; private set; } = 0;

        public float AveragePerSecond { get; private set; } = -1;
            
        public void Begin() {
            if (!started.IsRunning) started.Start();
            if (stopwatch.IsRunning) {
                End();
            }
            stopwatch.Restart();
            total.Start();
        }

        public void End() {
            if (!stopwatch.IsRunning) return;
            stopwatch.Stop();
            total.Stop();
            Last = stopwatch.ElapsedTicks;

            AveragePerSecond = total.ElapsedTicks / (float)started.ElapsedTicks;
                
                
            if (Last > Max) Max = Last;
            if (Count > 0) {
                Average -= Average / Count;
                Average += Last / Count;
            } else {
                Average = Last;
            }

            Count++;
        }

        public void Clear() {
            Average = -1;
            Count = 0;
            Last = -1;
            Max = -1;
            started.Reset();
            total.Reset();
        }
            
    }

    private static readonly Dictionary<string, PerformanceLog> Logs = new();

    public static void Begin(string key = null, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFileName = null) {
        var k = key;
        if (k == null && (callerFileName == null || callerMemberName == null)) return;
        k ??= $"{callerFileName}::{callerMemberName}";
        if (!Logs.ContainsKey(k)) Logs.Add(k, new PerformanceLog());
        Logs[k].Begin();
    }

    public static void End(string key = null, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFileName = null) {
        var k = key;
        if (k == null && (callerFileName == null || callerMemberName == null)) return;
        k ??= $"{callerFileName}::{callerMemberName}";
        if (!Logs.ContainsKey(k)) return;
        Logs[k].End();
    }

    public static void ClearAll() {
        foreach (var l in Logs) {
            l.Value.Clear();
        }
    }
        
}