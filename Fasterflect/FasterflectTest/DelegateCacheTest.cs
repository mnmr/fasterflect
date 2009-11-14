﻿#region License
// Copyright 2009 Buu Nguyen (http://www.buunguyen.net/blog)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://fasterflect.codeplex.com/
#endregion

using System;
using System.Collections;
using Fasterflect;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FasterflectTest
{
    [TestClass]
    public class DelegateCacheTest
    {
        internal class Person
        {
            private int id;
            private static int miles;
            private static int Miles
            {
                get { return miles; }
                set { miles = value; }
            }
            protected object this[string s]
            {
                get { return null; }
                set { }
            }
            public int Id { get; set; }
            public int Age { private get; set; }
            internal Person() { }
            internal Person(int id) { this.id = id; }
            private int GetId() { return id; }
            private void SetId(int id) { this.id = id; }
            private static void SetMiles(int m) { miles = m; }
            private static int GetMiles() { return miles; }
            private static int GetMiles(int offset) { return offset + miles; }
            private object GetItself(object data) { return data; }
            private void Run() { }
            private void Run(float speed) { }
            private static void Generate() { }
            private static void Generate(Person sample) { }
        }

        internal class Utils
        {
            private static void Swap(ref int i, ref int j)
            {
                int tmp = i;
                i = j;
                j = tmp;
            }
        }

        private Reflector reflector;
        private object target;
        private Type targetType;
        private IDictionary delegateMap;

        [TestInitialize()]
        public void TestInitialize()
        {
            target = new Person();
            targetType = typeof(Person);
            reflector = Reflector.Create();
            var cache = reflector.GetField<object>(reflector, "cache");
            delegateMap = reflector.GetField<IDictionary>(cache, "map");
        }

        private void Execute_cache_test(params Action[] actions)
        {
            int delCount = delegateMap.Count;
            foreach (var action in actions)
            {
                for (int i = 0; i < 20; i++) 
                    action();
                Assert.AreEqual(++delCount, delegateMap.Count);
            }
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_fields()
        {
            Execute_cache_test(
                () => reflector.SetField(target, "id", 1),
                () => reflector.GetField<int>(target, "id"),
                () => reflector.SetField(targetType, "miles", 1),
                () => reflector.GetField<int>(targetType, "miles"));
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_properties()
        {
            Execute_cache_test(
                () => reflector.SetProperty(target, "Age", 1),
                () => reflector.GetProperty<int>(target, "Age"),
                () => reflector.SetProperty(targetType, "Miles", 1),
                () => reflector.GetProperty<int>(targetType, "Miles"));
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_array_element()
        {
            Execute_cache_test(
                () => reflector.SetElement(targetType.MakeArrayType().Construct(10), 1, target),
                () => reflector.GetElement<object>(targetType.MakeArrayType().Construct(10), 1));
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_constructors()
        {
            Execute_cache_test(
                () => reflector.Construct(targetType),
                () => reflector.Construct(targetType, new[] { typeof(int) }, new object[] { 1 }));
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_methods()
        {
            Execute_cache_test(
                () => reflector.Invoke(targetType, "Generate"),
                () => reflector.Invoke<int>(targetType, "GetMiles"),
                () => reflector.Invoke(target, "SetId", new[] { typeof(int) }, new object[] { 1 }),
                () => reflector.Invoke<int>(target, "GetId"));
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_indexers()
        {
            Execute_cache_test(
                () => reflector.SetIndexer(target, new[] { typeof(string), typeof(object) }, new object[] { "a", null }),
                () => reflector.GetIndexer<string>(target, new[] { typeof(string) }, new object[] { "a"}));
        }

        [TestMethod]
        public void Test_delegate_is_property_cached_for_method_with_byref_params()
        {
            var action = reflector.DelegateForStaticInvoke(typeof (Utils), "Swap",
                                              new[]{typeof(int).MakeByRefType(),
                                                    typeof(int).MakeByRefType()});
            var parameters = new object[] {1, 2};
            action(parameters);
            Assert.AreEqual(2, parameters[0]);
            Assert.AreEqual(1, parameters[1]);
        }

        [TestMethod]
        public void Test_delegate_retrieval_methods_return_correct_delegate_type()
        {
            var funcs = new Func<Delegate>[]
                              {
                                  () => targetType.MakeArrayType().DelegateForGetElement(),
                                  () => targetType.MakeArrayType().DelegateForSetElement(),
                                  () => targetType.DelegateForConstruct(),
                                  () => targetType.DelegateForConstruct(new[] { typeof(int) }),
                                  () => targetType.DelegateForStaticInvoke("SetMiles", new[] { typeof(int) }),
                                  () => targetType.DelegateForStaticInvoke("GetMiles"),
                                  () => targetType.DelegateForStaticInvoke("GetMiles", new[] { typeof(int) }),
                                  () => targetType.DelegateForSetStaticField("miles"),
                                  () => targetType.DelegateForGetStaticField("miles"),
                                  () => targetType.DelegateForSetStaticProperty("Miles"),
                                  () => targetType.DelegateForGetStaticProperty("Miles"),
                                  () => targetType.DelegateForStaticInvoke("Generate"),
                                  () => targetType.DelegateForStaticInvoke("Generate", new[] { typeof(Person) }),
                                  () => targetType.DelegateForSetIndexer(new[] { typeof(string), typeof(object) }),
                                  () => targetType.DelegateForGetIndexer(new[] { typeof(string) }),
                                  () => targetType.DelegateForSetField("id"),
                                  () => targetType.DelegateForGetField("id"),
                                  () => targetType.DelegateForSetProperty("Id"),
                                  () => targetType.DelegateForGetProperty("Id"),
                                  () => targetType.DelegateForInvoke("SetId", new[] { typeof(int) }),
                                  () => targetType.DelegateForInvoke("GetId"),
                                  () => targetType.DelegateForInvoke("GetItself", new[] { typeof(object) }),
                                  () => targetType.DelegateForInvoke("Run"),
                                  () => targetType.DelegateForInvoke("Run", new[] { typeof(float) })
                              };
            var types = new[]
                              {
                                  typeof(ArrayElementGetter),
                                  typeof(ArrayElementSetter),
                                  typeof(ConstructorInvoker),
                                  typeof(ConstructorInvoker),
                                  typeof(StaticMethodInvoker),
                                  typeof(StaticMethodInvoker),
                                  typeof(StaticMethodInvoker),
                                  typeof(StaticAttributeSetter),
                                  typeof(StaticAttributeGetter),
                                  typeof(StaticAttributeSetter),
                                  typeof(StaticAttributeGetter),
                                  typeof(StaticMethodInvoker),
                                  typeof(StaticMethodInvoker),
                                  typeof(MethodInvoker),
                                  typeof(MethodInvoker),
                                  typeof(AttributeSetter),
                                  typeof(AttributeGetter),
                                  typeof(AttributeSetter),
                                  typeof(AttributeGetter),
                                  typeof(MethodInvoker),
                                  typeof(MethodInvoker),
                                  typeof(MethodInvoker),
                                  typeof(MethodInvoker),
                                  typeof(MethodInvoker)
                              };

            for (int i = 0; i < funcs.Length; i++)
            {
                var result = funcs[i]();
                Assert.IsNotNull(result);
                Assert.IsInstanceOfType(result, types[i]);
            }
        }
    }
}
