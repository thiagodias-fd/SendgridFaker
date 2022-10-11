using System.Text;

namespace SendgridFaker.PublicModels
{
    public class SendGridMessage
    {
        public EmailAddress From { get; set; }
        public string Subject { get; set; }
        public Personalization[] Personalizations { get; set; }
        public Content[] Content { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append("Subject: ").AppendLine(Subject);
            sb.Append("From: ").AppendLine(From.Email);
            sb.Append("To: ").AppendJoin(", ", Personalizations.Select(p => p.To)).AppendLine();
            sb.AppendLine("Contents: ");

            Content.Select((c, i) =>
            {
                sb.Append('[').Append(i).AppendLine("]");
                sb.Append("Type: ").AppendLine(c.Type);
                sb.AppendLine(c.Value);
                return string.Empty;
            });

            return sb.ToString();
        }
    }
}