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

    public static bool operator ==(Dog left, Dog right) => Operator.Weave(left, right);
    public static bool operator !=(Dog left, Dog right) => Operator.Weave(left, right);
}
```
What gets compiled:
```csharp
class Dog : IEquatable<Dog>
{
    public string Name { get; set; }
    public int Age { get; set; }
    public static bool operator ==(Dog left, Dog right) => object.Equals(left,right);
    public static bool operator !=(Dog left, Dog right) => !object.Equals(left,right);
    
    public override bool Equals(Dog other)
    {
        bool result = false;
        if (!object.ReferenceEquals(null, other))
        {
            if (object.ReferenceEquals(this, other))
            {
                result = true;
            }
            else if (object.Equals(Name, other.Name) && Age == other.Age)
            {
                result = true;
            }
        }
        return result;
    }
    
    public override bool Equals(object other)
    {
        bool result = false;
        if (!object.ReferenceEquals(null, other))
        {
            if (object.ReferenceEquals(this, other))
            {
                result = true;
            }
            else if (this.GetType() == other.GetType())
            {
                result = Equals((Dog)other);
            }
        }
        return result;
    }
    
    public override int GetHashCode()
    {
        int num = Name?.GetHashCode() ?? 0;
        return (num * 397) ^ Age;
    }
}
```
#### Installation 
1. Install the NuGet package: `PM> Install-Package PostSharp.Community.StructuralEquality`
2. Get a free PostSharp Community license at https://www.postsharp.net/essentials
3. When you compile for the first time, you'll be asked to enter the license key.

#### How to use
1. Add `[StructuralEquality]` to the classes where you want it to apply.
2. Choose, for each class, whether you want to also implement `==` and `!=` operators.

   If yes, add the following code to the class:
   ```csharp
   public static bool operator ==(YourClassName left, YourClassName right) => Operator.Weave(left, right);
   public static bool operator !=(YourClassName left, YourClassName right) => Operator.Weave(left, right);
   ```
   
   If no, use `[StructuralEquality(DoNotAddEqualityOperators=true)]` instead.


#### Details
* The add-in creates an override for `Equals` and `GetHashCode`, it implements `IEquatable<ClassName>` and creates a strongly-typed `Equals` method to implement the `IEquatable` interface. 
  * If any of those methods already exist, they're used and the add-in doesn't overwrite them. 
* The `Equals` and `GetHashCode` methods take into account all fields in the class, including backing fields of auto-implemented properties, except for fields and properties marked with `[IgnoreDuringEquals]`. They don't take into account fields in the base class, but they do call the base class's `Equals` and `GetHashCode`, if it exists, unless you use `IgnoreBaseClass`.
* You can combine this add-in's automatic logic with your custom logic by annotating methods in the class with `[AdditionalEqualsMethod]` and `[AdditionalGetHashCodeMethod]`. 
* The default settings, if you choose no options, are very close to the default implementation created by ReSharper's *Generate equality members*.
* For fields that implement `IEnumerable`, in `Equals`, the equality is for each element (it's deep equality). The same is true for `GetHashCode`.
* Adding the `==` and `!=` operators will result in compiler warnings CS0660 and CS0661, which tell you to add custom Equals and GetHashCode implementations. This add-in is doing this for you, but only after the compiler runs. To suppress these false-positives, you can either do so:
    * per project, by adding `<PropertyGroup><NoWarn>CS0660;CS0661</NoWarn></PropertyGroup>` to the project file
    * per source file, by adding `#pragma warning disable CS0660, CS0661`

#### Advanced case
Your code:
```csharp
[StructuralEquality(TypeCheck = TypeCheck.ExactlyTheSameTypeAsThis)]
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
    
    public static bool operator ==(AdvancedCase left, AdvancedCase right) => Operator.Weave(left, right);
    public static bool operator !=(AdvancedCase left, AdvancedCase right) => Operator.Weave(left, right);
}

[StructuralEquality(DoNotAddEqualityOperators = true, DoNotAddGetHashCode = true)]
public class AdvancedBaseClass
{
    private int baseField;
}
```
What gets compiled, and why:
```csharp
public class AdvancedCase : AdvancedBaseClass, IEquatable<AdvancedCase>
{
  protected string field;
  public List<List<object>> lists { get; } = new List<List<object>>();
  public float DoNotUse { get; set; }

  [AdditionalEqualsMethod]
  public bool AndFloatWithinRange(AdvancedCase other) => Math.Abs(DoNotUse - other.DoNotUse) < 0.1f;

  // Operators are transformed into actual code:
  public static bool operator ==(AdvancedCase left, AdvancedCase right) => object.Equals(left, right);
  public static bool operator !=(AdvancedCase left, AdvancedCase right) => !object.Equals(left, right);

  public override bool Equals(AdvancedCase other)
  {
    bool result = false;
    if (!object.ReferenceEquals(null, other))
    {
      if (object.ReferenceEquals(this, other))
      {
        result = true;
      }
      else if (base.Equals((object)other) && // base.Equals is called
               object.Equals(field, other.field) && // the "DoNotUse" field is not compared, it was excluded
               CollectionHelper.Equals(lists, other.lists) && // collections are compared element-by-element 
               AndFloatWithinRange(other)) // the custom logic is appended at the end of the Equals method
      {
        result = true;
      }
    }
    return result;
  }

  public override bool Equals(object other)
  {
    bool result = false;
    if (!object.ReferenceEquals(null, other))
    {
      if (object.ReferenceEquals(this, other))
      {
        result = true;
      }
      else if ((object)GetType() == other.GetType())
      {
        result = Equals((AdvancedCase)other);
      }
    }
    return result;
  }

  public override int GetHashCode()
  {
    int num = field?.GetHashCode() ?? 0;
    if (lists != null)
    {
      // hash code is element-by-element for collections  
      IEnumerator enumeratorVariable = ((IEnumerable)lists).GetEnumerator();
      while (enumeratorVariable.MoveNext())
      {
        num = ((num * 397) ^ (enumeratorVariable.Current?.GetHashCode() ?? 0));
      }
    }
    return num;
  }
}

public class AdvancedBaseClass : IEquatable<AdvancedBaseClass>
{
  // No GetHashCode is created because of DoNotAddGetHashCode
  // The operators == and != still mean referential equality because of
  //  DoNotAddEqualityOperators.

  private int baseField;

  public override bool Equals(AdvancedBaseClass other)
  {
    bool result = false;
    if (!object.ReferenceEquals(null, other))
    {
      if (object.ReferenceEquals(this, other))
      {
        result = true;
      }
      else if (baseField == other.baseField)
      {
        result = true;
      }
    }
    return result;
  }

  public override bool Equals(object other)
  {
    bool result = false;
    if (!object.ReferenceEquals(null, other))
    {
      if (object.ReferenceEquals(this, other))
      {
        result = true;
      }
      else if ((object)GetType() == other.GetType())
      {
        result = Equals((AdvancedBaseClass)other);
      }
    }
    return result;
  }
}
```

#### Copyright notices
Published under the MIT license.

* Copyright © PostSharp Technologies, Rafał Jasica, Simon Cropp, and contributors 
* Icon by Pixel Buddha, https://www.flaticon.com