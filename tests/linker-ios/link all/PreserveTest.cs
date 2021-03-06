//
// Preserve tests
//
// Authors:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// Copyright 2013-2016 Xamarin Inc. All rights reserved.
//

using System;
using System.Reflection;
#if XAMCORE_2_0
using Foundation;
using ObjCRuntime;
#else
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
#endif
using NUnit.Framework;

// this will preserve the specified type (only)
[assembly: Preserve (typeof (LinkAll.Attributes.TypeWithoutMembers))]

// this will preserve the specified type with all it's members
[assembly: Preserve (typeof (LinkAll.Attributes.TypeWithMembers), AllMembers = true)]

// as the preserved field is an attribute this means that [Obfuscation] becomes like [Preserve]
// IOW preserving the attribute does not do much good if what it decorates gets removed
[assembly: Preserve (typeof (ObfuscationAttribute))]

namespace LinkAll.Attributes {

	// type and members preserved by assembly-level attribute above
	class TypeWithMembers {

		public string Present { get; set; }
	}

	// type (only, not members) preserved by assembly-level attribute above
	class TypeWithoutMembers {

		public string Absent { get; set; }
	}

	class MemberWithCustomAttribute {

		// since [Obfuscation] was manually preserved then we'll preserve everything that's decorated with the attribute
		[Obfuscation]
		public string Custom { get; set; }
	}

	[TestFixture]
	// we want the tests to be available because we use the linker
	[Preserve (AllMembers = true)]
	public class PreserveTest {

#if DEBUG
		const bool Debug = true;
#else
		const bool Debug = false;
#endif
#if XAMCORE_2_0
		const string NamespacePrefix = "";
		string AssemblyName = typeof (NSObject).Assembly.ToString ();
#else
		const string NamespacePrefix = "MonoTouch.";
		const string AssemblyName = "monotouch";
#endif

		[Test]
		public void PreserveTypeWithMembers ()
		{
			var t = Type.GetType ("LinkAll.Attributes.TypeWithMembers");
			// both type and members are preserved
			Assert.NotNull (t, "type");
			Assert.NotNull (t.GetProperty ("Present"), "members");
		}

		[Test]
		public void PreserveTypeWithoutMembers ()
		{
			var t = Type.GetType ("LinkAll.Attributes.TypeWithoutMembers");
			// type is preserved
			Assert.NotNull (t, "type");
			// but we did not ask the linker to preserve it's members
			Assert.Null (t.GetProperty ("Absent"), "members");
		}

		[Test]
		public void PreserveTypeWithCustomAttribute ()
		{
			var t = Type.GetType ("LinkAll.Attributes.MemberWithCustomAttribute");
			// both type and members are preserved - in this case the type is preserved because it's member was
			Assert.NotNull (t, "type");
			// and that member was preserved because it's decorated with a preserved attribute
			Assert.NotNull (t.GetProperty ("Custom"), "members");
		}

		[Test]
		public void Class_LookupFullName ()
		{
			var klass = Type.GetType (NamespacePrefix + "ObjCRuntime.Class, " + AssemblyName);
			Assert.NotNull (klass, "Class");
			// only required (and preserved) for debug builds
			var method = klass.GetMethod ("LookupFullName", BindingFlags.NonPublic | BindingFlags.Static);
			// note: since iOS/runtime unification this is being called by ObjCRuntime.Runtime.LookupManagedTypeName
			// and will never be removed (even on Release builds)
			Assert.NotNull (method, "LookupFullName");
		}

		[Test]
		public void Runtime_RegisterEntryAssembly ()
		{
			var klass = Type.GetType (NamespacePrefix + "ObjCRuntime.Runtime, " + AssemblyName);
			Assert.NotNull (klass, "Runtime");
			// RegisterEntryAssembly is only needed for the simulator (not on devices) so it's only preserved for sim builds
			var method = klass.GetMethod ("RegisterEntryAssembly", BindingFlags.NonPublic | BindingFlags.Static, null, new [] { typeof (Assembly) }, null);
			Assert.That (method == null, Is.EqualTo (Runtime.Arch == Arch.DEVICE), "RegisterEntryAssembly");
		}

		[Test]
		public void MonoTouchException_Unconditional ()
		{
			var klass = Type.GetType (NamespacePrefix + "Foundation.MonoTouchException, " + AssemblyName);
			Assert.NotNull (klass, "MonoTouchException");
		}

		[Test]
		public void Class_Unconditional ()
		{
			var klass = Type.GetType (NamespacePrefix + "ObjCRuntime.Class, " + AssemblyName);
			Assert.NotNull (klass, "Class");
			// handle is unconditionally preserved
			var field = klass.GetField ("handle", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull (field, "handle");
		}

		[Test]
		public void Runtime_Unconditional ()
		{
			var klass = Type.GetType (NamespacePrefix + "ObjCRuntime.Runtime, " + AssemblyName);
			Assert.NotNull (klass, "Runtime");
			// Initialize, RegisterAssembly... are unconditionally preserved
			var method = klass.GetMethod ("Initialize", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull (method, "Initialize");
			method = klass.GetMethod ("RegisterAssembly", BindingFlags.Public | BindingFlags.Static);
			Assert.NotNull (method, "RegisterAssembly");
			method = klass.GetMethod ("RegisterNSObject", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof (NSObject), typeof (IntPtr) }, null);
			Assert.NotNull (method, "RegisterNSObject");
			method = klass.GetMethod ("GetBlockWrapperCreator", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof (MethodInfo), typeof (int) }, null);
			Assert.NotNull (method, "GetBlockWrapperCreator");
			method = klass.GetMethod ("CreateBlockProxy", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof (MethodInfo), typeof (IntPtr) }, null);
			Assert.NotNull (method, "CreateBlockProxy");
		}

		[Test]
		public void Selector_Unconditional ()
		{
			var klass = Type.GetType (NamespacePrefix + "ObjCRuntime.Selector, " + AssemblyName);
			Assert.NotNull (klass, "Selector");
			// handle and is unconditionally preserved
			var field = klass.GetField ("handle", BindingFlags.NonPublic | BindingFlags.Instance);
			Assert.NotNull (field, "handle");
			var method = klass.GetMethod ("GetHandle", BindingFlags.Public | BindingFlags.Static);
			Assert.NotNull (method, "GetHandle");
		}
	}
}