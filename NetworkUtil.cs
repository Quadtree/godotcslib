/**
 * This file is released under the MIT License: https://opensource.org/licenses/MIT
 */
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

class NetworkUtil
{
    public static bool IsInDedicatedServerMode()
    {
        foreach (var arg in OS.GetCmdlineArgs())
        {
            var parts = arg.Split("=");

            if (parts[0] == "--server") return true;
        }

        return false;
    }
}