using System;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.Enums;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.GenericClass;
using PostSharp.Community.StructuralEquality.Tests.Fody.AssemblyToProcess.InheritedClass;
using Xunit;

namespace PostSharp.Community.StructuralEquality.Tests.Fody.IntegrationTests
{
    public class GetHashCodeTests
    {
        [Fact]
        public void GetHashCode_should_return_value_for_class_with_generic_property()
        {
            var instance = new GenericProperty<int> {Prop = 1};
            Assert.NotEqual(0, instance.GetHashCode());
        }

        [Fact]
        public void GetHashCode_should_return_value_for_empty_type()
        {
            var instance = new EmptyClass();
            var result = instance.GetHashCode();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_null_string()
        {
            var instance = new SimpleClass();
            instance.Text = null;

            var result = instance.GetHashCode();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_null_nullable()
        {
            var instance = new ClassWithNullable();
            instance.NullableDate = null;

            var result = instance.GetHashCode();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_date_nullable()
        {
            var instance = new ClassWithNullable();
            instance.NullableDate = new DateTime(1988, 5, 23);

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_different_value_for_changed_property_in_base_class()
        {
            var instance = new InheritedClass();
            instance.A = 1;
            instance.B = 2;

            var firstResult = instance.GetHashCode();
            instance.A = 3;
            var secondResult = instance.GetHashCode();

            Assert.NotEqual(firstResult, secondResult);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_struct()
        {
            var instance = new SimpleStruct();
            instance.X = 1;
            instance.Y = 2;

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_guid_class()
        {
            var instance = new GuidClass();
            instance.Key = Guid.NewGuid();

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_normal_class()
        {
            var instance = new NormalClass();
            instance.X = 1;
            instance.Y = "2";
            instance.Z = 4.5;
            instance.V = 'C';

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_should_ignored_marked_properties()
        {
            var instance = new IgnoredPropertiesClass();
            instance.X = 1;
            instance.Y = 2;

            var firstResult = instance.GetHashCode();
            instance.Y = 3;
            var secondResult = instance.GetHashCode();

            Assert.Equal(firstResult, secondResult);
        }

        [Fact]
        public void GetHashCode_should_should_ignored_inherited_marked_properties()
        {
            var instance = new InheritedIgnoredPropertiesClass();
            instance.X = 1;
            instance.Y = 2;

            var firstResult = instance.GetHashCode();
            instance.Y = 3;
            var secondResult = instance.GetHashCode();

            Assert.Equal(firstResult, secondResult);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_array()
        {
            var instance = new IntCollection();
            instance.Collection = new[] {1, 2, 3, 4, 5, 6};
            instance.Count = 2;

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_int_array()
        {
            var instance = new IntArray();
            instance.Collection = new[] {1, 2, 3};

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_string_array()
        {
            var instance = new StringArray();
            instance.Collection = new[] {"one", "two", "three"};

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_null_array()
        {
            var instance = new IntCollection();
            instance.Collection = null;
            instance.Count = 0;

            var result = instance.GetHashCode();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_empty_array()
        {
            var instance = new IntCollection();
            instance.Collection = new int[0];
            instance.Count = 0;

            var result = instance.GetHashCode();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_type_with_only_array()
        {
            var instance = new OnlyIntCollection();
            instance.Collection = new[] {1, 2, 3, 4, 5};

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_generic_class()
        {
            var instance = new GenericClass<GenericClassNormalClass>();
            instance.A = 1;
            var propInstance = new GenericClassNormalClass();
            var array = new GenericClassNormalClass[1];
            array[0] = propInstance;

            instance.B = array;

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_enums()
        {
            var instance = new EnumClass(3,6);

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_nested_class()
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

            var result = nestedInstance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_class_without_generic_parameter()
        {
            var instance = new WithoutGenericParameter {Z = 12, A = 1};
            var propInstance = new GenericClassNormalClass();
            var array = new GenericClassNormalClass[1];
            array[0] = propInstance;
            instance.B = array;

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_class_with_generic_parameter()
        {
            var instance = new WithGenericParameter<GenericClassNormalClass>();
            instance.X = 12;
            instance.A = 1;
            var propInstance = new GenericClassNormalClass();
            var array = new GenericClassNormalClass[1];
            array[0] = propInstance;
            instance.B = array;

            var result = instance.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_class_with_static_properties()
        {
            var first = new ClassWithStaticProperties();
            first.X = 1;
            first.Y = "2";

            var result = first.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_class_with_indexer()
        {
            var first = new ClassWithIndexer();
            first.X = 1;
            first.Y = 2;

            var result = first.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_return_value_for_class_with_generic_property2()
        {
            var first = new ClassWithGenericProperty();
            first.Prop = new GenericDependency<int> {Prop = 1};

            var result = first.GetHashCode();

            Assert.NotEqual(0, result);
        }

        [Fact]
        public void GetHashCode_should_ignore_properties_in_base_class_when_class_is_marked()
        {
            var instance = new IgnoreBaseClass();
            instance.A = 1;
            instance.B = 2;

            var instance2 = new IgnoreBaseClass();
            instance2.A = 3;
            instance2.B = 2;

            var first = instance.GetHashCode();
            var second = instance2.GetHashCode();

            Assert.Equal(first, second);
        }
    }
}

