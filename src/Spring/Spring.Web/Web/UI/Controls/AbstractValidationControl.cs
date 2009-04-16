#region License

/*
 * Copyright � 2002-2008 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

#region Imports

using System;
using System.Collections;
using System.Diagnostics;
using System.Web.UI;
using System.Web.UI.WebControls;
using Spring.Context;
using Spring.Util;
using Spring.Validation;
using Spring.Web.UI.Validation;

#endregion

namespace Spring.Web.UI.Controls
{
    /// <summary>
    /// Provides common functionality to all validation renderer controls.
    /// </summary>
    /// <author>Erich Eichinger</author>
    public abstract class AbstractValidationControl : Control
    {
        private string provider;
        private string validationContainerName;
        private IValidationErrorsRenderer renderer;

#if !NET_2_0
        private bool initialized;

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
        /// </summary>
        protected override void OnInit(System.EventArgs e)
        {
            initialized = true;
            base.OnInit(e);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is in design mode.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is in design mode; otherwise, <c>false</c>.
        /// </value>
        protected bool DesignMode
        {
            get
            {
                if (this.Site != null)
                {
                    return this.Site.DesignMode;
                }
                return (this.Context == null) && initialized;
            }
        }
#endif
        /// <summary>
        /// If set, <see cref="ValidationContainer"/> will resolve to the named control specified
        /// by this property. The behavior of name resolution is identical to 
        /// <see cref="System.Web.UI.WebControls.BaseValidator.ControlToValidate"/>, except that if the name
        /// starts with "::", the resolution will start at the page level instead of relative to this
        /// control
        /// </summary>
        public virtual string ValidationContainerName
        {
            get { return validationContainerName; }
            set { validationContainerName = value; }
        }

        /// <summary>
        /// Gets or sets the provider.
        /// </summary>
        /// <value>The provider.</value>
        public virtual string Provider
        {
            get
            {
                if (this.provider == null)
                {
                    this.provider = this.ID;
                    if (this.provider == null)
                    {
                        this.provider = string.Empty;
                    }
                }
                return this.provider;
            }
            set
            {
                AssertUtils.ArgumentNotNull(value, "Provider");
                this.provider = value;
            }
        }

        /// <summary>
        /// Gets or sets the validation errors renderer to use. 
        /// </summary>
        /// <remarks>
        /// If not explicitly specified, defaults to <see cref="SpanValidationErrorsRenderer"/>.
        /// </remarks>
        /// <value>The validation errors renderer to use.</value>
        public IValidationErrorsRenderer Renderer
        {
            get
            {
                if (this.renderer == null)
                {
                    this.renderer = CreateValidationErrorsRenderer();
                    AssertUtils.ArgumentNotNull(this.renderer, "Renderer", "CreateValidationErrorsRenderer must not return null");
                }
                return this.renderer;
            }
            set
            {
                AssertUtils.ArgumentNotNull(value, "Renderer");
                this.renderer = value;
            }
        }

        /// <summary>
        /// Gets the MessageSource to be used for resolve error messages
        /// </summary>
        /// <remarks>
        /// By default, returns <see cref="ValidationContainer"/>'s MessageSource.
        /// </remarks>
        protected virtual IMessageSource MessageSource
        {
            get { return ValidationContainer == null ? null : ValidationContainer.MessageSource; }
        }

        /// <summary>
        /// Create the default <see cref="IValidationErrorsRenderer"/> 
        /// for this ValidationControl if none is configured.
        /// </summary>
        protected abstract IValidationErrorsRenderer CreateValidationErrorsRenderer();

        /// <summary>
        /// Gets the <see cref="IValidationContainer"/>, who's <see cref="IValidationContainer.ValidationErrors"/> 
        /// shall be rendered by this control.
        /// </summary>
        protected virtual IValidationContainer ValidationContainer
        {
            get
            {
                // is an explicit container specified?
                if (ValidationContainerName != null)
                {
                    Control start = this.NamingContainer;
                    string containerName = this.ValidationContainerName;
                    // shall we do a global search?
                    if (containerName.StartsWith("::"))
                    {
                        containerName = containerName.Substring(2);
                        start = this.Page;
                    }
                    IValidationContainer container = start.FindControl(containerName) as IValidationContainer;
                    if (container == null)
                    {
                        throw new ArgumentException(string.Format("Validation Container Control specified by {0} does not exist or does not implement IValidationContainer", this.ValidationContainerName));
                    }
                    return container;
                }

                for (Control parent = this.Parent; parent != null; parent = parent.Parent)
                {
                    IValidationContainer container = parent as IValidationContainer;
                    if (container != null
                        && container.ValidationErrors != null)
                    {
                        return container;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Resolves the <see cref="ValidationContainer"/>'s list of validation errors to a list
        /// of <see cref="string"/> elements containing the error messages to be rendered.
        /// </summary>
        /// <returns>a list containing <see cref="string"/> elements. May return <c>null</c></returns>
        protected virtual IList ResolveErrorMessages()
        {
            IList errorMessages;

            // good catch - idea & patch from Roberto Paterlini
            if (DesignMode)
            {
                errorMessages = new string[] { GetType().Name + ":" + ID };
                return errorMessages;
            }

            IValidationContainer container = this.ValidationContainer;
            if (container == null)
            {
                return null;
            }

            IValidationErrors validationErrors = container.ValidationErrors;
            if (validationErrors == null)
            {
                return null;
            }

            errorMessages = validationErrors.GetResolvedErrors(this.Provider, this.MessageSource);
            return errorMessages;
        }

        /// <summary>
        /// Renders error messages using the specified <see cref="Renderer"/>.
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            IList errorMessages;

            errorMessages = ResolveErrorMessages();

            Renderer.RenderErrors(Page as Page, writer, errorMessages);
        }
    }
}