//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
#nullable enable

using System;

namespace RepoM.Plugin.Statistics.ActionMenu.Context
{
    partial class UsageVariables
    {
        public override void RegisterFunctions(RepoM.ActionMenu.Interface.Scriban.IContextRegistration contextRegistration)
        {
            contextRegistration = contextRegistration.CreateOrGetSubRegistration("statistics");
            contextRegistration.RegisterFunction("count", (Func<RepoM.ActionMenu.Interface.ActionMenuFactory.IActionMenuGenerationContext, int>)GetCount);
            contextRegistration.RegisterFunction("overall_count", (Func<int>)GetOverallCount);
        }
    }
}
