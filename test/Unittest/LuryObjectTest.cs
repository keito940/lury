﻿using System;
using Lury.Core.Runtime;
using NUnit.Framework;

namespace Unittest
{
    [TestFixture]
    public class LuryObjectTest
    {
        [Test]
        public void AssignTest()
        {
            var baseObject = new LuryObject(null, null);
            var classObject = new LuryObject(baseObject, null, "BaseClass");
            var luryObject = new LuryObject(baseObject, classObject);

            var attributeObject1 = new LuryObject(baseObject, null, 1);
            var attributeObject2 = new LuryObject(baseObject, null, 2);
            var attributeObject3 = new LuryObject(baseObject, null, 3);
            var attributeObject4 = new LuryObject(baseObject, null, 4);

            luryObject.Assign("foo", attributeObject1);
            classObject.Assign("bar", attributeObject2);
            baseObject.Assign("hoge", attributeObject3);
            baseObject.Assign("fuga", attributeObject4);

            Assert.True(luryObject.Has("foo"));
            Assert.True(luryObject.Has("hoge"));
            Assert.True(luryObject.Has("fuga"));
            
            Assert.True(baseObject.Has("hoge"));
            Assert.True(baseObject.Has("fuga"));

            Assert.False(luryObject.Has("bar"));
            Assert.False(luryObject.Has("baz"));

            Assert.False(baseObject.Has("foo"));
            Assert.False(baseObject.Has("bar"));
        }
    }
}