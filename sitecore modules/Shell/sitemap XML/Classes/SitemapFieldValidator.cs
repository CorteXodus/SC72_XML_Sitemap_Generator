using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Validators;

namespace Sitecore.Modules.SitemapXML
{
	[System.Serializable]
    public class SitemapPriorityFieldValidator : StandardValidator
    {
        public override string Name
        {
            get
            {
                return "Sitemap Priority Range";
            }
        }

        public SitemapPriorityFieldValidator()
        {
        }

        public SitemapPriorityFieldValidator(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
		{
		}

        protected override ValidatorResult Evaluate()
        {
            string controlValidationValue = base.ControlValidationValue;
            if (string.IsNullOrEmpty(controlValidationValue))
            {
                return ValidatorResult.Valid;
            }

            float num;

            if (!float.TryParse(controlValidationValue, out num))
            {
                base.Text = base.GetText("Field \"{0}\" must contain a single-precision floating-point number. ex: 0.5", new string[]
				{
					base.GetFieldDisplayName()
				});
                return base.GetFailedResult(ValidatorResult.FatalError);
            }

            float minNum = 0.0F;
            float maxNum = 1.0F;

            if (num < minNum)
            {
             	base.Text = base.GetText("The field \"{0}\" must contain a value greater than or equal to {1}.", new string[]
				{
					base.GetFieldDisplayName(),
					minNum.ToString()
				});
				return base.GetFailedResult(ValidatorResult.FatalError);
			}

            if (num > maxNum)
            {
                base.Text = base.GetText("The field \"{0}\" must contain a value lesser than or equal to {1}.", new string[]
				{
					base.GetFieldDisplayName(),
					maxNum.ToString()
				});
                return base.GetFailedResult(ValidatorResult.FatalError);
            }

            return ValidatorResult.Valid;
        }
        protected override ValidatorResult GetMaxValidatorResult()
        {
            return base.GetFailedResult(ValidatorResult.FatalError);
        }
    }
}