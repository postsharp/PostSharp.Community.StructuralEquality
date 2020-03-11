## ![](icon.png) &nbsp; PostSharp.Community.StructuralEquality 
Add `[StructuralEquality]` to your classes to avoid writing `Equals` and `GetHashCode` methods.

PostSharp synthesizes a member-by-member structural equality implementation of `Equals` and `GetHashCode` of annotated classes and adds those implementations directly to your assembly so they don't clutter your code.

*This is an add-in for [PostSharp](https://postsharp.net). It modifies your assembly during compilation by using IL weaving.*
 
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

#### Copyright notices
Published under the MIT license.

* Copyright © PostSharp Technologies
* Icon by Pixel Buddha, https://www.flaticon.com