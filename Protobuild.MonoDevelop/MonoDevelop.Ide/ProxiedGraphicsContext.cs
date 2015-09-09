//
// ProxiedGraphicsContext.cs
//
// Author:
//       James Rhodes <jrhodes@redpointsoftware.com.au>
//
// Copyright (c) 2015 James Rhodes
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using OpenTK.Graphics;

namespace MonoDevelop.Ide
{
	public class ProxiedGraphicsContext : MarshalByRefObject, IGraphicsContext, IGraphicsContextInternal
	{
		private IGraphicsContext target;

		private IGraphicsContextInternal targetInternal;

		public ProxiedGraphicsContext(IGraphicsContext graphicsContext)
		{
			SetTarget(graphicsContext);
		}

		public void SetTarget(IGraphicsContext graphicsContext)
		{
			target = graphicsContext;
			targetInternal = (IGraphicsContextInternal)graphicsContext;
		}

		public void SwapBuffers ()
		{
			target.SwapBuffers();
		}

		public void MakeCurrent (OpenTK.Platform.IWindowInfo window)
		{
			if (window is ProxiedWindowInfo)
			{
				target.MakeCurrent((window as ProxiedWindowInfo).UnderlyingWindowInfo);
			}
			else
			{
				target.MakeCurrent(window);
			}
		}

		public void Update (OpenTK.Platform.IWindowInfo window)
		{
			if (window is ProxiedWindowInfo)
			{
				target.Update((window as ProxiedWindowInfo).UnderlyingWindowInfo);
			}
			else
			{
				target.Update(window);
			}
		}

		public void LoadAll ()
		{
			target.LoadAll();
		}

		public bool IsCurrent {
			get {
				return target.IsCurrent;
			}
		}

		public bool IsDisposed {
			get {
				return target.IsDisposed;
			}
		}

		public bool VSync {
			get {
				return target.VSync;
			}
			set {
				target.VSync = value;
			}
		}

		public int SwapInterval {
			get {
				return target.SwapInterval;
			}
			set {
				target.SwapInterval = value;
			}
		}

		public GraphicsMode GraphicsMode {
			get {
				return target.GraphicsMode;
			}
		}

		public bool ErrorChecking {
			get {
				return target.ErrorChecking;
			}
			set {
				target.ErrorChecking = value;
			}
		}

		public void Dispose ()
		{
			target.Dispose();
		}

		public IntPtr GetAddress (string function)
		{
			return targetInternal.GetAddress(function);
		}

		public IntPtr GetAddress (IntPtr function)
		{
			return targetInternal.GetAddress(function);
		}

		public IGraphicsContext Implementation {
			get {
				return this;
			}
		}

		public OpenTK.ContextHandle Context {
			get {
				return targetInternal.Context;
			}
		}
	}
}

