﻿using System.ComponentModel;
using System.Reflection;

namespace DataAccess.Shared; 

////
////https://lostechies.com/jimmybogard/2008/08/12/enumeration-classes/
///
///
/// 
[DefaultProperty(nameof(Value))]
public abstract class Enumeration : IComparable {
    public string Name { get; protected set; } = "";
    public string DisplayName { get; } = "";
    public int Value { get; }

    public override string ToString() => DisplayName;

    protected Enumeration() { }
    protected Enumeration(int value, string displayName) {
        this.Value = value;
        this.DisplayName = displayName;
    }

    //TODO memoize
    public static IEnumerable<T> GetAll<T>() where T : Enumeration, new() {
        var type = typeof(T);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        foreach (var info in fields) {
            var instance = new T();
            if (info.GetValue(instance) is T locatedValue) yield return locatedValue;
        }
    }

    public override bool Equals(object? obj) {
        if (obj is not Enumeration otherValue) return false;

        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Value.Equals(otherValue.Value);

        return typeMatches && valueMatches;
    }

    public override int GetHashCode() => Value.GetHashCode();

    public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue) {
        var absoluteDifference = Math.Abs(firstValue.Value - secondValue.Value);
        return absoluteDifference;
    }

    public static T FromValue<T>(int value) where T : Enumeration, new() {
        var matchingItem = parse<T, int>(value, "value", item => item.Value == value);
        return matchingItem;
    }

    public static T? TryFromValue<T>(int value) where T : Enumeration, new() => GetAll<T>().FirstOrDefault(item => item.Value == value);

    public static T FromDisplayName<T>(string displayName) where T : Enumeration, new() {
        var matchingItem = parse<T, string>(displayName, "display name", item => item.DisplayName == displayName);
        return matchingItem;
    }

    public static bool ContainsValue<T>(int value) where T : Enumeration, new() => 
        GetAll<T>().FirstOrDefault(x=>x.Value == value) is not null;

    private static T parse<T, K>(K value, string description, Func<T, bool> predicate) where T : Enumeration, new() {
        var matchingItem = GetAll<T>().FirstOrDefault(predicate);

        if (matchingItem is null) {
            var message = $"'{value}' is not a valid {description} in {typeof(T)}";
            throw new ApplicationException(message);
        }
        return matchingItem;
    }
    
    public int CompareTo(object? other) => other is null ? 1 : Value.CompareTo(((Enumeration)other).Value);

    public static bool operator ==(Enumeration? left, Enumeration? right) => left is null ? right is null : left.Value == right?.Value;

    public static bool operator !=(Enumeration left, Enumeration right) => !(left == right);
}