using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using PEPEngineers.PEPErvice.Data;
using PEPEngineers.PEPErvice.Extensions;
using PEPEngineers.PEPErvice.Interfaces;
using UnityEngine;
using UnityEngine.Assertions;
using ISubsystem = PEPEngineers.PEPErvice.Interfaces.ISubsystem;

namespace PEPEngineers.PEPErvice.Runtime
{
	public sealed class SubsystemLocator : ILocator, IRegister, IDisposable
	{
		private readonly Dictionary<Type, ISubsystem> registeredSubsystems = new();
		private readonly HashSet<Type> sceneOnlyTypes = new();
		private readonly Dictionary<Type, Func<ISubsystem>> subsystemFactories = new();
		private readonly Action<ISubsystem> disposeSubsystem;
		private bool disposed;

		public SubsystemLocator() : this(subsystem => subsystem.Dispose())
		{
		}

		internal SubsystemLocator(Action<ISubsystem> disposeSubsystem)
		{
			this.disposeSubsystem = disposeSubsystem ?? throw new ArgumentNullException(nameof(disposeSubsystem));
		}

		public IReadOnlyDictionary<Type, ISubsystem> RegisteredSubsystems => registeredSubsystems;
		public IReadOnlyDictionary<Type, Func<ISubsystem>> SubsystemFactories => subsystemFactories;
		public IReadOnlyCollection<Type> SceneOnlyTypes => sceneOnlyTypes;

		public ISubsystem GetSubsystem(Type type)
		{
			ThrowIfDisposed();
			ValidateType(type);
			if (registeredSubsystems.TryGetValue(type, out var subsystem) && subsystem.IsAlive())
				return subsystem;
			registeredSubsystems.Remove(type);

			if (!subsystemFactories.TryGetValue(type, out var factory)) return null;
			subsystem = factory();
			if (!subsystem.IsAlive() || !type.IsInstanceOfType(subsystem))
				throw new InvalidOperationException($"The factory for {type.FullName} returned a null, destroyed, disposed or incompatible subsystem.");
			registeredSubsystems[type] = subsystem;
			return subsystem;
		}

		public T GetSubsystem<T>() where T : ISubsystem
		{
			var subsystem = GetSubsystem(TypeCache<T>.Value);
			return subsystem == null ? default : (T)subsystem;
		}

		public ILocator GetSubsystem<T>(out T value) where T : ISubsystem
		{
			value = GetSubsystem<T>();
			return this;
		}

		public IRegister BindSubsystem(Type type, Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton)
		{
			ThrowIfDisposed();
			ValidateType(type);
			Assert.IsNotNull(resolver);
			if (resolver == null) throw new ArgumentNullException(nameof(resolver));
			subsystemFactories[type] = resolver;
			if (lifetime == Lifetime.Scene) sceneOnlyTypes.Add(type);
			else sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
			return this;
		}

		public IRegister BindSubsystem<T>(Func<ISubsystem> resolver, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem
		{
			return BindSubsystem(TypeCache<T>.Value, resolver, lifetime);
		}

		public void UnbindSubsystem(Type type)
		{
			ValidateType(type);
			subsystemFactories.Remove(type);
			sceneOnlyTypes.Remove(type);
			RemoveRegisteredType(type);
		}

		public void UnbindSubsystem<T>() where T : ISubsystem
		{
			UnbindSubsystem(TypeCache<T>.Value);
		}

		public IRegister RegisterSubsystem(Type type, ISubsystem subsystem, Lifetime lifetime = Lifetime.Singleton)
		{
			ThrowIfDisposed();
			ValidateType(type);
			Assert.IsNotNull(subsystem);
			if (!subsystem.IsAlive()) throw new ArgumentNullException(nameof(subsystem));
			if (!type.IsInstanceOfType(subsystem))
				throw new ArgumentException($"The subsystem does not implement {type.FullName}.", nameof(subsystem));
			subsystemFactories.Remove(type);
			registeredSubsystems[type] = subsystem;
			if (lifetime == Lifetime.Scene) sceneOnlyTypes.Add(type);
			else sceneOnlyTypes.Remove(type);
			return this;
		}

		public IRegister RegisterSubsystem<T>([NotNull] T subsystem, Lifetime lifetime = Lifetime.Singleton) where T : ISubsystem
		{
			return RegisterSubsystem(TypeCache<T>.Value, subsystem, lifetime);
		}

		public void UnregisterSystem<T>() where T : ISubsystem
		{
			UnbindSubsystem<T>();
		}

		private void RemoveRegisteredType(Type type)
		{
			if (registeredSubsystems.Remove(type, out var subsystem)) DisposeSubsystem(subsystem);
		}

		public void RemoveSceneSystems()
		{
			ThrowIfDisposed();
			foreach (var type in sceneOnlyTypes) RemoveRegisteredType(type);
		}

		public void Clear()
		{
			ThrowIfDisposed();
			ClearSubsystems();
		}

		public void Dispose()
		{
			if (disposed) return;
			disposed = true;
			ClearSubsystems();
		}

		private void ClearSubsystems()
		{
			foreach (var subsystem in registeredSubsystems.Values.Distinct()) DisposeSubsystem(subsystem);
			registeredSubsystems.Clear();
			sceneOnlyTypes.Clear();
			subsystemFactories.Clear();
		}

		private void DisposeSubsystem(ISubsystem subsystem)
		{
			if (!subsystem.IsAlive()) return;
			try
			{
				disposeSubsystem(subsystem);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void ThrowIfDisposed()
		{
			if (disposed) throw new ObjectDisposedException(nameof(SubsystemLocator));
		}

		private static void ValidateType(Type type)
		{
			Assert.IsNotNull(type);
			if (type == null) throw new ArgumentNullException(nameof(type));
			Assert.IsTrue(typeof(ISubsystem).IsAssignableFrom(type));
			if (!typeof(ISubsystem).IsAssignableFrom(type))
				throw new ArgumentException($"{type.FullName} is not a subsystem type.", nameof(type));
		}
	}
}
