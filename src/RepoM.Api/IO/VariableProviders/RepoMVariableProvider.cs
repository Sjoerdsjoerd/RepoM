namespace RepoM.Api.IO.VariableProviders;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using RepoM.Api.IO.Variables;
using ExpressionStringEvaluator.VariableProviders;
using RepoM.Api.IO.ModuleBasedRepositoryActionProvider.Data;

interface IItem
{
}

interface IItemHandler<in T> where T : IItem
{
    object? Handle(T item, object? value);
}

struct ArraySelector : IItem
{
    public ArraySelector(int index)
    {
        Index = index;
    }

    public int Index { get; set; }
}

struct PropertySelector : IItem
{
    public PropertySelector(string property)
    {
        Property = property;
    }

    public string Property { get; set; }
}

class ArrayHandler : IItemHandler<ArraySelector>
{
    public object? Handle(ArraySelector item, object? value)
    {
        if (value is not IList list)
        {
            return null;
        }

        if (list.Count <= item.Index)
        {
            return null;
        }

        return list[item.Index];

    }
}

class PropertyHandler : IItemHandler<PropertySelector>
{
    public object? Handle(PropertySelector item, object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is ExpandoObject eo)
        {
            return eo.SingleOrDefault(pair => pair.Key == item.Property).Value;
        }

        try
        {
            return value.GetType().GetProperty(item.Property)?.GetValue(value, null)
                   ??
                   Array.Find(
                       value.GetType().GetProperties(),
                       p =>
                           p.CanRead
                           &&
                           item.Property.Equals(p.Name, StringComparison.InvariantCultureIgnoreCase))
                    ?.GetValue(value, null);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return null;
    }
}

public class RepoMVariableProvider : RepoM.Core.Plugin.VariableProviders.IVariableProvider
{
    private static readonly char[] _separatorChars = { '.', '[', };
    private const string PREFIX = "var.";

    private readonly PropertyHandler _propertyHandler = new();
    private readonly ArrayHandler _arrayHandler = new();

    /// <inheritdoc cref="IVariableProvider.CanProvide"/>
    public bool CanProvide(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (key.Length <= PREFIX.Length)
        {
            return false;
        }

        if (!key.StartsWith(PREFIX, StringComparison.CurrentCultureIgnoreCase))
        {
            return false;
        }

        var envKey = key[PREFIX.Length..];
        return !string.IsNullOrWhiteSpace(envKey);
    }
    
    /// <inheritdoc cref="IVariableProvider.Provide"/>
    public object? Provide(string key, string? arg)
    {
        var envKey = key[PREFIX.Length..];
        var envSearchKey = envKey;
        var index = envKey.IndexOfAny(_separatorChars);
        if (index > 0)
        {
            envSearchKey = envKey[..index];
        }

        Scope? scope = RepoMVariableProviderStore.VariableScope.Value;

        while (true)
        {
            if (scope == null)
            {
                return null;
            }

            if (!TryGetValueFromScope(scope, envSearchKey, out var result))
            {
                scope = scope.Parent;
                continue;
            }

            if (index < 0)
            {
                return result;
            }

            IEnumerable<IItem> selectors = FindSelectors(envKey[index..]);

            foreach (IItem selector in selectors)
            {
                result = HandlePropertySelector(selector, result);
            }

            return result;
        }
    }

    private object? HandlePropertySelector(IItem selector, object? r)
    {
        if (selector is PropertySelector ps)
        {
            r = _propertyHandler.Handle(ps, r);
        }

        else if (selector is ArraySelector @as)
        {
            r = _arrayHandler.Handle(@as, r);
        }

        return r;
    }

    private static IEnumerable<IItem> FindSelectors(string selector)
    {
        var dots = selector.TrimStart('.').Split('.').ToList();
        foreach (var dot in dots)
        {
            var arrays = dot.Split('[').ToList();
            
            if (arrays.Count > 1)
            {
                if (!string.IsNullOrWhiteSpace(arrays[0]))
                {
                    yield return new PropertySelector(arrays[0]);
                }

                // for now, only first
                var index = arrays[1].TrimEnd(']');
                yield return new ArraySelector(int.Parse(index));
            }
            else
            {
                yield return new PropertySelector(arrays[0]);
            }
        }
    }

    private static bool TryGetValueFromScope(in Scope scope, string key, out object? value)
    {
        EvaluatedVariable? var = scope.Variables.Find(x => key.Equals(x.Name, StringComparison.CurrentCultureIgnoreCase));

        if (var != null)
        {
            value = var.Value;
            return var.Value != null;
        }

        value = null;
        return false;
    }
}