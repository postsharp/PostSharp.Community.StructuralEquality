using System;
using System.Collections.Generic;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.Enums;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.GenericClass;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.InheritedClass;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.IntegrationTests
{
    public class EqualsTests
    {
        [Fact]
        public void Equals_should_return_value_for_class_with_generic_property()
        {
            var first = new GenericProperty<int>();
            first.Prop = 1;
            var second = new GenericProperty<int>();
            second.Prop = 1;
            var third = new GenericProperty<int>();
            third.Prop = 2;

            Assert.True(first.Equals(second));
            Assert.False(first.Equals(third));
        }

        [Fact]
        public void Equals_should_return_true_for_StructWithArray()
        {
            var first = new StructWithArray();
            first.X = new[] {1, 2};
            first.Y = new[] {3, 4};
            var second = new StructWithArray();
            second.X = new[] {1, 2};
            second.Y = new[] {3, 4};

            Assert.True(first.Equals(second));
        }

        [Fact]
        public void Equals_should_return_false_for_StructWithArray()
        {
            var first = new StructWithArray();
            first.X = new[] {1, 2};
            first.Y = new[] {3, 4};
            var second = new StructWithArray();
            second.X = new[] {1, 2};
            second.Y = new[] {1, 4};

            Assert.False(first.Equals(second));
        }

        [Fact]
        public void Equals_should_return_value_for_class_without_generic_parameter()
        {
            var instance = new WithoutGenericParameter();
            instance.Z = 12;
            instance.A = 1;
            var propInstance = new GenericClassNormalClass();
            var array = new GenericClassNormalClass[1];
            array[0] = propInstance;
            instance.B = array;

            var instance2 = new WithoutGenericParameter();
            instance2.Z = 12;
            instance2.A = 1;
            var array2 = new GenericClassNormalClass[1];
            var propInstance2 = new GenericClassNormalClass();
            array2[0] = propInstance2;
            instance2.B = array2;

            var result = instance.Equals(instance2);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_value_for_class_with_generic_parameter()
        {

            var instance = new WithGenericParameter<GenericClassNormalClass>();
            instance.X = 12;
            instance.A = 1;
            var propInstance = new GenericClassNormalClass();
            var array = new GenericClassNormalClass[1];
            array[0] = propInstance;
            instance.B = array;

            var instance2 = new WithGenericParameter<GenericClassNormalClass>();
            instance2.X = 12;
            instance2.A = 1;
            var propInstance2 = new GenericClassNormalClass();
            var array2 = new GenericClassNormalClass[1];
            array2[0] = propInstance2;
            instance2.B = array;

            bool result = instance.Equals(instance2);

            Assert.True(result);
        }

        bool CheckEqualityOnTypesForTypeCheck(string left, string right)
        {
            left = "PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess." + left;
            right = "PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess." + right;
            var leftType = this.GetType().Assembly.GetType(left);
            dynamic leftInstance = Activator.CreateInstance(leftType);
            leftInstance.A = 1;

            var rightType = this.GetType().Assembly.GetType(right);
            dynamic rightInstance = Activator.CreateInstance(rightType);
            rightInstance.A = 1;

            return leftInstance.Equals((object) rightInstance);
        }

        [Theory]
        [InlineData("EqualsOrSubtypeClass", "EqualsOrSubtypeClass", true)]
        [InlineData("EqualsOrSubtypeClass", "EqualsOrSubtypeSubClass", true)]
        [InlineData("EqualsOrSubtypeSubClass", "EqualsOrSubtypeClass", true)]
        [InlineData("EqualsOrSubtypeSubClass", "EqualsOrSubtypeSubClass", true)]
        [InlineData("ExactlyOfTypeClass", "ExactlyOfTypeClass", true)]
        [InlineData("ExactlyOfTypeSubClass", "ExactlyOfTypeClass", false)]
        [InlineData("ExactlyOfTypeClass", "ExactlyOfTypeSubClass", true)]
        [InlineData("ExactlyOfTypeSubClass", "ExactlyOfTypeSubClass", false)]
        [InlineData("ExactlyTheSameTypeAsThisClass", "ExactlyTheSameTypeAsThisClass", true)]
        [InlineData("ExactlyTheSameTypeAsThisClass", "ExactlyTheSameTypeAsThisSubClass", false)]
        [InlineData("ExactlyTheSameTypeAsThisSubClass", "ExactlyTheSameTypeAsThisClass", false)]
        
        [InlineData("EqualsOrSubtypeStruct", "EqualsOrSubtypeStruct", true)]
        [InlineData("ExactlyOfTypeStruct", "ExactlyOfTypeStruct", true)]
        [InlineData("ExactlyTheSameTypeAsThisStruct", "ExactlyTheSameTypeAsThisStruct", true)]
        [InlineData("EqualsOrSubtypeStruct", "ExactlyOfTypeStruct", false)]
        [InlineData("ExactlyOfTypeStruct", "EqualsOrSubtypeStruct", false)]
        [InlineData("ExactlyTheSameTypeAsThisStruct", "ExactlyOfTypeStruct", false)]
        //TODO: support sub classes
        //[InlineData("ExactlyTheSameTypeAsThisSubClass", "ExactlyTheSameTypeAsThisSubClass", true)]
        public void Equals_should_use_type_check_option(string left, string right, bool result)
        {
            Assert.Equal(result, CheckEqualityOnTypesForTypeCheck(left, right));
        }

        [Fact]
        public void Equals_should_return_true_for_empty_type()
        {
            var first = new EmptyClass();
            var second = new EmptyClass();

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_enums()
        {
            var first = new EnumClass(3,6);
            var second = new EnumClass(3,6);

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_generic_class()
        {
            Func<GenericClass<GenericClassNormalClass>> createInstance = () =>
            {
                var instance = new GenericClass<GenericClassNormalClass>();
                instance.A = 1;
                var propInstance = new GenericClassNormalClass();

                var array = new GenericClassNormalClass[1];
                array[0] = propInstance;

                instance.B = array;
                return instance;
            };
            var first = createInstance();
            var second = createInstance();

            bool result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_should_ignored_marked_properties()
        {
            var first = new IgnoredPropertiesClass();
            first.X = 1;
            first.Y = 2;

            var second = new IgnoredPropertiesClass();
            second.X = 1;
            second.Y = 3;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_should_inherited_ignored_marked_properties()
        {
            var first = new InheritedIgnoredPropertiesClass();
            first.X = 1;
            first.Y = 2;

            var second = new InheritedIgnoredPropertiesClass();
            second.X = 1;
            second.Y = 3;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_false_for_different_value_for_changed_property_in_base_class()
        {
            var first = new InheritedClass();
            first.A = 1;
            first.B = 2;

            var second = new InheritedClass();
            second.A = 3;
            second.B = 2;

            var result = first.Equals(second);

            Assert.False(result);
        }

        [Fact]
        public void Equals_should_return_true_for_class_with_indexer()
        {
            var first = new ClassWithIndexer();
            first.X = 1;
            first.Y = 2;

            var second = new ClassWithIndexer();
            second.X = 1;
            second.Y = 2;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_equal_collections()
        {
            var first = new IntCollection();
            first.Collection = new[] {1, 2, 3, 4, 5, 6};
            first.Count = 2;

            var second = new IntCollection();
            second.Collection = new List<int> {1, 2, 3, 4, 5, 6};
            second.Count = 2;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_equal_arrays()
        {
            var first = new IntArray();
            first.Collection = new[] {1, 2, 3};

            var second = new IntArray();
            second.Collection = new[] {1, 2, 3};

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_equal_string_arrays()
        {
            var first = new StringArray();
            first.Collection = new[] {"one", "two", "three"};

            var second = new StringArray();
            second.Collection = new[] {"one", "two", "three"};

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_reference_equal_array()
        {
            var first = new IntCollection();
            first.Collection = new[] {1, 2, 3, 4, 5, 6};
            first.Count = 2;

            var second = new IntCollection();
            second.Collection = first.Collection;
            second.Count = 2;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_null_array()
        {
            var first = new IntCollection();
            first.Collection = null;
            first.Count = 0;

            var second = new IntCollection();
            second.Collection = null;
            second.Count = 0;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_false_for_null_array_and_fill_array()
        {
            var first = new IntCollection();
            first.Collection = new[] {1};
            first.Count = 0;

            var second = new IntCollection();
            second.Collection = null;
            second.Count = 0;

            var result = first.Equals(second);

            Assert.False(result);
        }

        [Fact]
        public void Equals_should_return_false_for_fill_array_and_null_array()
        {
            var first = new IntCollection();
            first.Collection = null;
            first.Count = 0;

            var second =new IntCollection();
            second.Collection = new[] {1};
            second.Count = 0;

            var result = first.Equals(second);

            Assert.False(result);
        }

        [Fact]
        public void Equals_should_return_true_for_nested_class()
        {
            var nestedInstanceFirst = GetNestedClassInstance();
            var nestedInstanceSecond = GetNestedClassInstance();

            bool result = nestedInstanceFirst.Equals(nestedInstanceSecond);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_false_for_changed_nested_class()
        {
            var nestedInstanceFirst = GetNestedClassInstance();
            var nestedInstanceSecond = GetNestedClassInstance();
            nestedInstanceSecond.D.X = 11;

            bool result = nestedInstanceFirst.Equals(nestedInstanceSecond);

            Assert.False(result);
        }

        [Fact]
        public void Equals_should_return_false_for_null_nested_class()
        {
            var nestedInstanceFirst = GetNestedClassInstance();
            var nestedInstanceSecond = GetNestedClassInstance();
            nestedInstanceSecond.D = null;

            bool result = nestedInstanceFirst.Equals(nestedInstanceSecond);

            Assert.False(result);
        }

        NestedClass GetNestedClassInstance()
        {
            var normalInstance = new NormalClass();
            normalInstance.X = 1;
            normalInstance.Y = "2";
            normalInstance.Z = 4.5;
            normalInstance.V = 'V';
            var nestedInstance = new NestedClass();
            nestedInstance.A = 10;
            nestedInstance.B = "11";
            nestedInstance.C = 12.25;
            nestedInstance.D = normalInstance;
            return nestedInstance;
        }

        [Fact]
        public void Equals_should_return_true_for_equal_structs()
        {
            var first = new SimpleStruct();
            first.X = 1;
            first.Y = 2;
            var second = new SimpleStruct();
            second.X = 1;
            second.Y = 2;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_false_for_changed_struct()
        {
            var first = new SimpleStruct();
            first.X = 1;
            first.Y = 2;
            var second = new SimpleStruct();
            second.X = 1;
            second.Y = 3;

            var result = first.Equals(second);

            Assert.False(result);
        }

        [Fact]
        public void Equals_should_return_true_for_equal_struct_property()
        {
            var first = new StructPropertyClass();
            first.A = 1;
            var firstProperty = new SimpleStruct();
            firstProperty.X = 2;
            firstProperty.X = 3;
            first.B = firstProperty;
            var second = new StructPropertyClass();
            second.A = 1;
            var secondProperty = new SimpleStruct();
            secondProperty.X = 2;
            secondProperty.X = 3;
            second.B = secondProperty;

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_equal_normal_class()
        {
            var instance = new NormalClass();
            instance.X = 1;
            instance.Y = "2";
            instance.Z = 4.5;
            instance.V = 'C';

            object first = instance;

            instance = new NormalClass();
            instance.X = 1;
            instance.Y = "2";
            instance.Z = 4.5;
            instance.V = 'C';

            object second = instance;

            var result1 = ( first).Equals( second);
            var result = first.Equals(second);

            Assert.True(result);
            Assert.True(result1);
        }

        [Fact]
        public void Equals_should_return_true_for_class_with_static_properties()
        {
            var first = new ClassWithStaticProperties();
            first.X = 1;
            first.Y = "2";

            var second = new ClassWithStaticProperties();
            second.X = 1;
            second.Y = "2";

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_true_for_class_with_guid_in_parent()
        {
            var guid = "{f6ab1abe-5811-40e9-8154-35776d2e5106}";

            var first = new ReferenceObject();
            first.Name = "Test";
            first.Id = Guid.Parse(guid);

            var second = new ReferenceObject();
            second.Name = "Test";
            second.Id = Guid.Parse(guid);

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_return_for_class_with_generic_property()
        {
            var first = new ClassWithGenericProperty();
            first.Prop = new GenericDependency<int> {Prop = 1};

            var second = new ClassWithGenericProperty();
            second.Prop = new GenericDependency<int> {Prop = 1};

            var result = first.Equals(second);

            Assert.True(result);
        }

        [Fact]
        public void Equals_should_ignore_properties_in_base_class_when_class_is_marked()
        {
            var instance = new IgnoreBaseClass();
            instance.A = 1;
            instance.B = 2;

            var instance2 = new IgnoreBaseClass();
            instance2.A = 3;
            instance2.B = 2;

            var result = instance.Equals(instance2);

            Assert.True(result);
        }
    }
}