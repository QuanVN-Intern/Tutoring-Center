using Microsoft.AspNetCore.Razor.TagHelpers;

namespace EduCenterManagement.TagHelpers
{
    [HtmlTargetElement("status-badge")]
    public class StatusBadgeTagHelper : TagHelper
    {
        public string Status { get; set; } = string.Empty;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "span";

            string badgeClass = "badge ";
            string displayText = Status;

            switch (Status.ToLower())
            {
                case "active":
                case "open":
                case "paid":
                case "completed":
                case "present":
                case "approved":
                    badgeClass += "bg-success text-white";
                    break;

                case "closed":
                case "unpaid":
                case "dropped":
                case "unexcusedabsent":
                case "rejected":
                    badgeClass += "bg-danger text-white";
                    break;

                case "pending":
                case "scheduled":
                case "excusedabsent":
                    badgeClass += "bg-warning text-dark";
                    break;

                default:
                    badgeClass += "bg-secondary text-white";
                    break;
            }

            output.Attributes.SetAttribute("class", badgeClass + " px-2 py-1 rounded-pill");
            output.Content.SetContent(displayText);
        }
    }
}
