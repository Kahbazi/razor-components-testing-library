﻿using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Diffing.Core;
using AngleSharp.Dom;
using Egil.RazorComponents.Testing.Asserting;
using Egil.RazorComponents.Testing.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Egil.RazorComponents.Testing
{
    /// <summary>
    /// Represents an abstract <see cref="IRenderedFragment"/> with base functionality.
    /// </summary>
    public abstract class RenderedFragmentBase : IRenderedFragment
    {
        private string? _snapshotMarkup;
        private string? _latestRenderMarkup;
        private INodeList? _firstRenderNodes;
        private INodeList? _latestRenderNodes;
        private INodeList? _snapshotNodes;

        /// <summary>
        /// Gets the id of the rendered component or fragment.
        /// </summary>
        protected abstract int ComponentId { get; }

        /// <summary>
        /// Gets the first rendered markup.
        /// </summary>
        protected abstract string FirstRenderMarkup { get; }

        /// <summary>
        /// Gets the container that handles the (re)rendering of the fragment.
        /// </summary>
        protected ContainerComponent Container { get; }

        /// <inheritdoc/>
        public ITestContext TestContext { get; }

        /// <summary>
        /// Creates an instance of the <see cref="RenderedFragmentBase"/> class.
        /// </summary>
        public RenderedFragmentBase(ITestContext testContext, RenderFragment renderFragment)
        {
            if (testContext is null) throw new ArgumentNullException(nameof(testContext));

            TestContext = testContext;
            Container = new ContainerComponent(testContext.Renderer);
            Container.Render(renderFragment);
            testContext.Renderer.OnRenderingHasComponentUpdates += ComponentMarkupChanged;
        }

        /// <inheritdoc/>
        public void SaveSnapshot()
        {
            _snapshotNodes = null;
            _snapshotMarkup = GetMarkup();
        }

        /// <inheritdoc/>
        public IReadOnlyList<IDiff> GetChangesSinceSnapshot()
        {
            if (_snapshotMarkup is null)
                throw new InvalidOperationException($"No snapshot exists to compare with. Call {nameof(SaveSnapshot)} to create one.");

            if(_snapshotNodes is null)
                _snapshotNodes = TestContext.HtmlParser.Parse(_snapshotMarkup);

            return GetNodes().CompareTo(_snapshotNodes);
        }


        /// <inheritdoc/>
        public IReadOnlyList<IDiff> GetChangesSinceFirstRender()
        {
            if (_firstRenderNodes is null)
                _firstRenderNodes = TestContext.HtmlParser.Parse(FirstRenderMarkup);
            return GetNodes().CompareTo(_firstRenderNodes);
        }


        /// <inheritdoc/>
        public string GetMarkup()
        {
            if (_latestRenderMarkup is null)
                _latestRenderMarkup = Htmlizer.GetHtml(TestContext.Renderer, ComponentId);
            return _latestRenderMarkup;
        }

        /// <inheritdoc/>
        public INodeList GetNodes()
        {
            if (_latestRenderNodes is null)
                _latestRenderNodes = TestContext.HtmlParser.Parse(GetMarkup());
            return _latestRenderNodes;
        }

        private void ComponentMarkupChanged(in RenderBatch renderBatch)
        {
            if (renderBatch.HasUpdatesTo(ComponentId) || HasChildComponentUpdated(renderBatch))
            {
                ResetLatestRenderCache();
            }
        }

        private bool HasChildComponentUpdated(in RenderBatch renderBatch)
        {
            var frames = TestContext.Renderer.GetCurrentRenderTreeFrames(ComponentId);
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames.Array[i];

                if (renderBatch.HasUpdatesTo(frame.ComponentId))
                {
                    return true;
                }

            }
            return false;
        }

        private void ResetLatestRenderCache()
        {
            _latestRenderMarkup = null;
            _latestRenderNodes = null;
        }
    }
}
