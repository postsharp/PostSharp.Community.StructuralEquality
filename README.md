## ![](icon.png) &nbsp; PostSharp.Community.StructuralEquality 
Add `[StructuralEquality]` to your classes to avoid writing `Equals` and `GetHashCode` methods.

PostSharp synthesizes a member-by-member structural equality implementation of `Equals` and `GetHashCode` of annotated classes and adds those implementations directly to your assembly so they don't clutter your code.

*This is an add-in for [PostSharp](https://postsharp.net). It modifies your assembly during compilation by using IL weaving. The add-in functionality is in preview, and not yet public. This add-in might not work and is unsupported.*
 
#### Example
Your code:
```csharp
[StructuralEquality]
class Dog
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```
What gets compiled:
```csharp
[StructuralEquality]
class Dog
{
    public string Name { get; set; }
    public int Age { get; set; }
    public override bool Equals(object right)
    {
        TODO add true implementation
    }

    public override int GetHashCode()
    {
        return TODO add true implementation
    }
}
```
#### Installation 
1. Install the NuGet package: `PM> Install-Package PostSharp.Community.StructuralEquality`
2. Get a free PostSharp Community license at https://www.postsharp.net/essentials
3. When you compile for the first time, you'll be asked to enter the license key.

#### How to use
Add `[StructuralEquality]` to the classes where you want it to apply.

#### Details
* The add-in creates an override for `Equals` and `GetHashCode`, it implements `IEquatable<ClassName>` and creates a strongly-typed `Equals` method to implement the `IEquatable` interface. 
  * If any of those methods already exist, they're used and the add-in doesn't overwrite them. 
* The `Equals` and `GetHashCode` method take into account all fields in the class, including backing fields of auto-implemented properties, except for fields and properties marked with `[IgnoreDuringEquals]`. They don't take into account fields in the base class, but they do call the base class's `Equals` and `GetHashCode`, if it exists, unless you use `IgnoreBaseClass`.
* You can combine this add-in's automatic logic with your custom logic by annotating methods in the class with `[AdditionalEqualsMethod]` and `[AdditionalGetHashCodeMethod]`. 
* The default settings, if you choose no options, is very close to the default implementation created by ReSharper's Generate equality members.
* For fields that implement `IEnumerable`, in `Equals`, the equality is for each element (it's deep equality). The same is true for `GetHashCode`.

#### Advanced case
Your code:
```csharp
[StructuralEquality(StringComparisonStyle = StringComparison.CurrentCultureIgnoreCase, TypeCheck = TypeCheck.ExactlyTheSameTypeAsThis)]
public class AdvancedCase : AdvancedBaseClass
{
    protected string field;
    public List<List<object>> lists { get; }= new List<List<object>>();
    [IgnoreDuringEquals]
    public float DoNotUse { get; set; }

    [AdditionalEqualsMethod]
    public bool AndFloatWithinRange(AdvancedCase other)
    {
        return Math.Abs(this.DoNotUse - other.DoNotUse) < 0.1f;
    }
}

[StructuralEquality(DoNotAddEqualityOperators = true, DoNotAddGetHashCode = true)]
public class AdvancedBaseClass
{
    private int baseField;
}
```
What gets compiled, and why:
```csharp
TODO
```

#### Copyright notices
Published under the MIT license.

* Copyright © PostSharp Technologies, Rafał Jasica, Simon Cropp, and contributors 
* Icon by Pixel Buddha, https://www.flaticon.com