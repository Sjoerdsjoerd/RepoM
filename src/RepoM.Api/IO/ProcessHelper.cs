namespace RepoM.Api.IO;

using System;
using System.Diagnostics;

public static class ProcessHelper
{
    public static void StartProcess(string process, string arguments)
    {
        try
        {
            Process.Start(process, arguments);
            return;
        }
        catch (Exception)
        {
            // swallow, retry below.
        }

        try
        {
            var psi = new ProcessStartInfo(process, arguments)
                {
                    UseShellExecute = true,
                };
            Process.Start(psi);
        }
        catch (Exception)
        {
            // swallow for now.
        }
    }
}