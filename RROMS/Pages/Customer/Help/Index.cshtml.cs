using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RROMS.Pages.Customer.Help
{
    // Protected by the CustomerOnly folder convention in Program.cs.
    public class IndexModel : PageModel
    {
        private static readonly IReadOnlyList<ChatbotRule>
            Rules = BuildRules();

        [BindProperty]
        [Required(
            ErrorMessage =
                "Please enter a question.")]
        [StringLength(
            300,
            ErrorMessage =
                "Your question cannot exceed 300 characters.")]
        public string Question { get; set; } =
            string.Empty;

        public ChatbotResponse? BotResponse
        {
            get;
            private set;
        }

        public IReadOnlyList<string> SuggestedQuestions
        {
            get;
        } = new List<string>
        {
            "How do I book a table?",
            "How do I track my order?",
            "Where can I find my receipt?",
            "Can I pay with cash?",
            "How do I leave feedback?",
            "How do I change my password?"
        };

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrWhiteSpace(Question))
            {
                ModelState.AddModelError(
                    nameof(Question),
                    "Please enter a question.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Question = Question.Trim();

            BotResponse = FindResponse(Question);

            return Page();
        }

        private static ChatbotResponse FindResponse(
            string question)
        {
            string normalizedQuestion =
                Normalize(question);

            ChatbotRule? bestRule =
                Rules
                    .Select(rule => new
                    {
                        Rule = rule,
                        Score = CalculateScore(
                            normalizedQuestion,
                            rule)
                    })
                    .Where(result =>
                        result.Score > 0)
                    .OrderByDescending(result =>
                        result.Score)
                    .ThenByDescending(result =>
                        result.Rule.Priority)
                    .Select(result =>
                        result.Rule)
                    .FirstOrDefault();

            if (bestRule == null)
            {
                return new ChatbotResponse
                {
                    Intent =
                        "Fallback",

                    Title =
                        "I could not match that question",

                    Answer =
                        "Try asking about reservations, " +
                        "the menu, your cart, checkout, " +
                        "payments, orders, receipts, " +
                        "history, notifications, feedback, " +
                        "or profile settings.",

                    Steps = new[]
                    {
                        "Use one of the suggested questions below.",
                        "Keep the question short and mention the feature you need help with.",
                        "Use the Customer sidebar if you already know which page you need."
                    }
                };
            }

            return new ChatbotResponse
            {
                Intent =
                    bestRule.Intent,

                Title =
                    bestRule.Title,

                Answer =
                    bestRule.Answer,

                Steps =
                    bestRule.Steps,

                ActionText =
                    bestRule.ActionText,

                ActionPage =
                    bestRule.ActionPage
            };
        }

        private static int CalculateScore(
            string normalizedQuestion,
            ChatbotRule rule)
        {
            int score = 0;

            foreach (string phrase in rule.Phrases)
            {
                string normalizedPhrase =
                    Normalize(phrase);

                if (normalizedQuestion.Contains(
                    normalizedPhrase,
                    StringComparison.Ordinal))
                {
                    int phraseWordCount =
                        normalizedPhrase
                            .Split(
                                ' ',
                                StringSplitOptions
                                    .RemoveEmptyEntries)
                            .Length;

                    score +=
                        4 + phraseWordCount;
                }
            }

            foreach (string keyword in rule.Keywords)
            {
                string normalizedKeyword =
                    Normalize(keyword);

                if (ContainsWholeWord(
                    normalizedQuestion,
                    normalizedKeyword))
                {
                    score += 1;
                }
            }

            return score;
        }

        private static bool ContainsWholeWord(
            string normalizedText,
            string normalizedKeyword)
        {
            string paddedText =
                $" {normalizedText} ";

            string paddedKeyword =
                $" {normalizedKeyword} ";

            return paddedText.Contains(
                paddedKeyword,
                StringComparison.Ordinal);
        }

        private static string Normalize(
            string value)
        {
            string normalized =
                value
                    .Trim()
                    .ToLowerInvariant();

            char[] characters =
                normalized
                    .Select(character =>
                        char.IsLetterOrDigit(character) ||
                        char.IsWhiteSpace(character)
                            ? character
                            : ' ')
                    .ToArray();

            return string.Join(
                ' ',
                new string(characters)
                    .Split(
                        ' ',
                        StringSplitOptions
                            .RemoveEmptyEntries));
        }

        private static IReadOnlyList<ChatbotRule>
            BuildRules()
        {
            return new List<ChatbotRule>
            {
                new()
                {
                    Intent =
                        "CreateReservation",

                    Priority =
                        20,

                    Phrases = new[]
                    {
                        "book a table",
                        "make a reservation",
                        "reserve a table",
                        "new reservation",
                        "create reservation"
                    },

                    Keywords = new[]
                    {
                        "book",
                        "reserve",
                        "reservation",
                        "table"
                    },

                    Title =
                        "Book a table",

                    Answer =
                        "Open My Reservations and select " +
                        "Book a Table. Choose a future date, " +
                        "time, and number of guests before " +
                        "submitting the reservation.",

                    Steps = new[]
                    {
                        "Open My Reservations from the Customer sidebar.",
                        "Select Book a Table.",
                        "Choose the dining date, time, and number of guests.",
                        "Review the details and submit the reservation."
                    },

                    ActionText =
                        "Open My Reservations",

                    ActionPage =
                        "/Customer/Reservations/Index"
                },

                new()
                {
                    Intent =
                        "EditReservation",

                    Priority =
                        30,

                    Phrases = new[]
                    {
                        "edit my reservation",
                        "change my reservation",
                        "update my booking",
                        "change booking time",
                        "change booking date"
                    },

                    Keywords = new[]
                    {
                        "edit",
                        "change",
                        "update",
                        "reservation",
                        "booking"
                    },

                    Title =
                        "Edit a reservation",

                    Answer =
                        "Open My Reservations and select Edit " +
                        "on an eligible reservation. Update the " +
                        "available details and save the changes.",

                    Steps = new[]
                    {
                        "Open My Reservations.",
                        "Find the reservation you want to update.",
                        "Select Edit.",
                        "Update the details and save the reservation."
                    },

                    ActionText =
                        "View My Reservations",

                    ActionPage =
                        "/Customer/Reservations/Index"
                },

                new()
                {
                    Intent =
    "CancelReservation",

Priority =
    70,

Phrases = new[]
{
    "how do i cancel my booking",
    "how can i cancel my booking",
    "how do i cancel my reservation",
    "how can i cancel my reservation",
    "cancel my booking",
    "cancel my reservation",
    "cancel a booking",
    "cancel a reservation",
    "remove my reservation",
    "cancel table booking",
    "booking cancellation",
    "reservation cancellation"
},

Keywords = new[]
{
    "cancel",
    "cancellation"
},

                    Title =
                        "Cancel a reservation",

                    Answer =
                        "Open My Reservations, find the eligible " +
                        "booking, and select Cancel. Review the " +
                        "confirmation carefully before completing " +
                        "the cancellation.",

                    Steps = new[]
                    {
                        "Open My Reservations.",
                        "Find the booking you want to cancel.",
                        "Select Cancel.",
                        "Confirm the cancellation when prompted."
                    },

                    ActionText =
                        "View My Reservations",

                    ActionPage =
                        "/Customer/Reservations/Index"
                },

                new()
                {
                    Intent =
                        "ReservationStatus",

                    Priority =
                        15,

                    Phrases = new[]
                    {
                        "reservation status",
                        "booking status",
                        "what does pending mean",
                        "what does confirmed mean",
                        "why is my reservation pending"
                    },

                    Keywords = new[]
                    {
                        "pending",
                        "confirmed",
                        "cancelled",
                        "reservation",
                        "status"
                    },

                    Title =
                        "Reservation status guide",

                    Answer =
                        "Pending means the reservation is awaiting " +
                        "restaurant confirmation. Confirmed means " +
                        "the booking has been accepted. Cancelled " +
                        "means the booking is no longer active.",

                    Steps = new[]
                    {
                        "Open My Reservations to view the latest status.",
                        "Check the status badge on the reservation card.",
                        "Use Notifications for important booking updates."
                    },

                    ActionText =
                        "Check Reservations",

                    ActionPage =
                        "/Customer/Reservations/Index"
                },

                new()
                {
                    Intent =
    "ReservationDuration",

Priority =
    55,

Phrases = new[]
{
    "how long is my booking",
    "how long is my reservation",
    "how long is the booking",
    "how long is the reservation",
    "how long is reservation",
    "how long is booking",
    "reservation duration",
    "booking duration",
    "two hour slot",
    "two hours",
    "dining slot",
    "reservation time limit",
    "booking time limit"
},

Keywords = new[]
{
    "duration",
    "hours",
    "hour",
    "slot",
    "long",
    "limit"
},

                    Title =
                        "Reservation duration",

                    Answer =
                        "Each Dineflow reservation uses a fixed " +
                        "two-hour dining slot. Checkout and payment " +
                        "require a current reservation that has not " +
                        "already ended.",

                    Steps = new[]
                    {
                        "Check the reservation date and start time.",
                        "Allow two hours from the displayed start time.",
                        "Place linked food orders before the reservation slot ends."
                    },

                    ActionText =
                        "View Reservations",

                    ActionPage =
                        "/Customer/Reservations/Index"
                },

                new()
                {
                    Intent =
                        "BrowseMenu",

                    Priority =
                        15,

                    Phrases = new[]
                    {
                        "browse the menu",
                        "view food",
                        "see dishes",
                        "what food is available",
                        "show menu"
                    },

                    Keywords = new[]
                    {
                        "menu",
                        "food",
                        "dish",
                        "drink",
                        "available"
                    },

                    Title =
                        "Browse the menu",

                    Answer =
                        "Open Menu to browse dishes by category. " +
                        "Available items show quantity and Add to " +
                        "Cart controls. Unavailable items cannot " +
                        "currently be ordered.",

                    Steps = new[]
                    {
                        "Open Menu from the Customer sidebar.",
                        "Browse categories or use the category shortcuts.",
                        "Choose a quantity for an available item.",
                        "Select Add to Cart."
                    },

                    ActionText =
                        "Browse Menu",

                    ActionPage =
                        "/Customer/Menu/Index"
                },

                new()
                {
                    Intent =
                        "CartHelp",

                    Priority =
                        20,

                    Phrases = new[]
                    {
                        "add to cart",
                        "update cart",
                        "remove from cart",
                        "clear my cart",
                        "view my cart"
                    },

                    Keywords = new[]
                    {
                        "cart",
                        "add",
                        "remove",
                        "quantity",
                        "clear"
                    },

                    Title =
                        "Manage your cart",

                    Answer =
                        "Add available dishes from Menu, then open " +
                        "My Cart to update quantities, remove items, " +
                        "clear the cart, or continue to checkout.",

                    Steps = new[]
                    {
                        "Add available items from Menu.",
                        "Open My Cart.",
                        "Update quantities or remove unwanted items.",
                        "Select Proceed to Checkout when ready."
                    },

                    ActionText =
                        "Open My Cart",

                    ActionPage =
                        "/Customer/Cart/Index"
                },

                new()
                {
                    Intent =
                        "CheckoutHelp",

                    Priority =
                        25,

                    Phrases = new[]
                    {
                        "how to checkout",
                        "checkout requirements",
                        "why do i need reservation",
                        "choose reservation for order",
                        "continue to payment"
                    },

                    Keywords = new[]
                    {
                        "checkout",
                        "reservation",
                        "payment",
                        "requirement"
                    },

                    Title =
                        "Complete checkout",

                    Answer =
                        "Checkout requires at least one available " +
                        "cart item and a current reservation. The " +
                        "reservation links the order to the correct " +
                        "table and dining time.",

                    Steps = new[]
                    {
                        "Confirm your cart contains available items.",
                        "Select Proceed to Checkout.",
                        "Choose one of your current reservations.",
                        "Continue to Payment."
                    },

                    ActionText =
                        "Review My Cart",

                    ActionPage =
                        "/Customer/Cart/Index"
                },

                new()
{
    Intent =
        "CardPaymentDeclined",

    Priority =
        60,

    Phrases = new[]
    {
        "card payment was declined",
        "card payment declined",
        "payment was declined",
        "payment declined",
        "card was declined",
        "card declined",
        "card payment failed",
        "payment failed",
        "cannot pay by card",
        "card not working"
    },

    Keywords = new[]
    {
        "card",
        "payment",
        "declined",
        "failed"
    },

    Title =
        "Card payment was declined",

    Answer =
    "A declined simulated Card payment does not create an " +
    "order or payment record, and the payment attempt does " +
    "not clear the cart. Check the entered card number and " +
    "try again. In Dineflow's simulation, a card number " +
    "ending in 0000 is always declined.",

    Steps = new[]
    {
        "Stay on the Payment page.",
        "Check that the card number was entered correctly.",
        "Use a simulated card number that does not end in 0000.",
        "Submit the Card payment again, or choose Cash on Arrival.",
        "If you leave the page, return through Cart and Checkout."
    },

    ActionText =
        "Return to My Cart",

    ActionPage =
        "/Customer/Cart/Index"
},

                new()
                {
                    Intent =
                        "CardPayment",

                    Priority =
                        30,

                    Phrases = new[]
{
    "pay by card",
    "card payment",
    "online payment",
    "how does card payment work",
    "can i use a card"
},


                    Keywords = new[]
{
    "card",
    "pay",
    "payment",
    "online"
},

                    Title =
                        "Card payment",

                    Answer =
                        "Choose Card Payment on the Payment page and " +
                        "enter a simulated card number. No real card " +
                        "is charged. A successful simulation creates " +
                        "a Paid payment record and receipt.",

                    Steps = new[]
                    {
                        "Complete Cart and Checkout first.",
                        "Choose Card Payment.",
                        "Enter the simulated card number.",
                        "Submit the payment and wait for the result."
                    },

                    ActionText =
                        "Open My Cart",

                    ActionPage =
                        "/Customer/Cart/Index"
                },

                new()
                {
                    Intent =
                        "CashPayment",

                    Priority =
                        35,

                    Phrases = new[]
                    {
                        "pay with cash",
                        "cash on arrival",
                        "pay at restaurant",
                        "cash payment",
                        "payment pending"
                    },

                    Keywords = new[]
                    {
                        "cash",
                        "arrival",
                        "restaurant",
                        "payment",
                        "pending"
                    },

                    Title =
                        "Cash on arrival",

                    Answer =
                        "Choose Cash on Arrival on the Payment page. " +
                        "The order is placed with a Pending payment " +
                        "status, and payment is collected at the " +
                        "restaurant.",

                    Steps = new[]
                    {
                        "Complete Cart and Checkout.",
                        "Choose Cash on Arrival.",
                        "Submit the order.",
                        "Pay the restaurant when you arrive."
                    },

                    ActionText =
                        "Open My Cart",

                    ActionPage =
                        "/Customer/Cart/Index"
                },

                new()
                {
                    Intent =
                        "OrderTracking",

                    Priority =
                        30,

                    Phrases = new[]
                    {
                        "track my order",
                        "where is my order",
                        "is my food ready",
                        "order progress",
                        "order status"
                    },

                    Keywords = new[]
                    {
                        "track",
                        "order",
                        "progress",
                        "ready",
                        "preparing",
                        "served",
                        "completed"
                    },

                    Title =
                        "Track an order",

                    Answer =
                        "Open My Orders to see the latest order " +
                        "status and progress tracker. The page checks " +
                        "for status updates automatically.",

                    Steps = new[]
                    {
                        "Open My Orders.",
                        "Find the relevant order number.",
                        "Review the status pill and progress tracker.",
                        "Use Check Now if you need a full page refresh."
                    },

                    ActionText =
                        "View My Orders",

                    ActionPage =
                        "/Customer/Orders/Index"
                },

                new()
                {
                    Intent =
                        "OrderStatus",

                    Priority =
                        20,

                    Phrases = new[]
                    {
                        "what does preparing mean",
                        "what does ready to serve mean",
                        "order status meaning",
                        "pending preparing ready served completed"
                    },

                    Keywords = new[]
                    {
                        "pending",
                        "preparing",
                        "ready",
                        "served",
                        "completed",
                        "order"
                    },

                    Title =
                        "Order status guide",

                    Answer =
                        "Pending means the order has been received. " +
                        "Preparing means Staff are preparing it. " +
                        "Ready to Serve means it is ready for the " +
                        "table. Served means it has been delivered. " +
                        "Completed means the workflow has finished.",

                    Steps = new[]
                    {
                        "Open My Orders.",
                        "Find the order card.",
                        "Read the current status and highlighted progress step."
                    },

                    ActionText =
                        "Check Order Status",

                    ActionPage =
                        "/Customer/Orders/Index"
                },

                new()
                {
                    Intent =
                        "ReceiptHelp",

                    Priority =
                        25,

                    Phrases = new[]
                    {
                        "find my receipt",
                        "view receipt",
                        "payment receipt",
                        "payment summary",
                        "print receipt"
                    },

                    Keywords = new[]
                    {
                        "receipt",
                        "summary",
                        "print",
                        "payment"
                    },

                    Title =
                        "Find a receipt or payment summary",

                    Answer =
                        "Open My Orders. Paid orders provide View " +
                        "Receipt, while pending Cash payments provide " +
                        "View Payment Summary. Paid receipts can be " +
                        "printed or saved as PDF.",

                    Steps = new[]
                    {
                        "Open My Orders.",
                        "Find the relevant order.",
                        "Select View Receipt or View Payment Summary.",
                        "Use Print / Save as PDF when available."
                    },

                    ActionText =
                        "View My Orders",

                    ActionPage =
                        "/Customer/Orders/Index"
                },

                new()
                {
                    Intent =
                        "HistoryHelp",

                    Priority =
                        15,

                    Phrases = new[]
                    {
                        "view my history",
                        "previous reservations",
                        "previous orders",
                        "previous payments",
                        "payment history"
                    },

                    Keywords = new[]
                    {
                        "history",
                        "previous",
                        "past",
                        "reservation",
                        "order",
                        "payment"
                    },

                    Title =
                        "View your history",

                    Answer =
                        "Open History to review Reservation, Order, " +
                        "and Payment records. You can switch tabs, " +
                        "filter by date, and move between pages.",

                    Steps = new[]
                    {
                        "Open History.",
                        "Choose Reservations, Orders, or Payments.",
                        "Optionally apply a date range.",
                        "Use pagination when more records are available."
                    },

                    ActionText =
                        "Open History",

                    ActionPage =
                        "/Customer/History/Index"
                },

                new()
                {
                    Intent =
                        "NotificationHelp",

                    Priority =
                        15,

                    Phrases = new[]
                    {
                        "view notifications",
                        "notification bell",
                        "unread notifications",
                        "mark as read",
                        "restaurant updates"
                    },

                    Keywords = new[]
                    {
                        "notification",
                        "notifications",
                        "unread",
                        "bell",
                        "updates"
                    },

                    Title =
                        "View notifications",

                    Answer =
                        "Open Notifications to review reservation, " +
                        "order, payment, and restaurant updates. " +
                        "Unread items can be marked individually or " +
                        "all at once.",

                    Steps = new[]
                    {
                        "Open Notifications or select the bell icon.",
                        "Review updates marked New.",
                        "Use Mark as Read or Mark All as Read."
                    },

                    ActionText =
                        "Open Notifications",

                    ActionPage =
                        "/Customer/Notifications/Index"
                },

                new()
                {
                    Intent =
                        "FeedbackHelp",

                    Priority =
                        20,

                    Phrases = new[]
                    {
                        "leave feedback",
                        "submit feedback",
                        "rate my experience",
                        "give rating",
                        "dining feedback"
                    },

                    Keywords = new[]
                    {
                        "feedback",
                        "rating",
                        "review",
                        "experience"
                    },

                    Title =
                        "Leave dining feedback",

                    Answer =
                        "Feedback becomes available for an eligible " +
                        "reservation after a linked order is " +
                        "Completed. Open My Reservations and select " +
                        "Leave Feedback.",

                    Steps = new[]
                    {
                        "Open My Reservations.",
                        "Find an eligible completed dining experience.",
                        "Select Leave Feedback.",
                        "Choose a rating and optionally add a comment.",
                        "Submit the feedback once."
                    },

                    ActionText =
                        "View My Reservations",

                    ActionPage =
                        "/Customer/Reservations/Index"
                },

                new()
                {
                    Intent =
                        "ChangePassword",

                    Priority =
                        20,

                    Phrases = new[]
                    {
                        "change my password",
                        "update password",
                        "new password",
                        "password settings",
                        "account security"
                    },

                    Keywords = new[]
                    {
                        "password",
                        "profile",
                        "security",
                        "change"
                    },

                    Title =
                        "Change your password",

                    Answer =
                        "Open Profile and use the Change Password " +
                        "section. Enter the current password, choose " +
                        "a different password with at least eight " +
                        "characters, and confirm it.",

                    Steps = new[]
                    {
                        "Open Profile.",
                        "Scroll to Change Password.",
                        "Enter the current password.",
                        "Enter and confirm the new password.",
                        "Select Change Password."
                    },

                    ActionText =
                        "Open Profile",

                    ActionPage =
                        "/Customer/Profile/Index"
                }
            };
        }

        public sealed class ChatbotRule
        {
            public string Intent { get; init; } =
                string.Empty;

            public int Priority { get; init; }

            public IReadOnlyList<string> Phrases
            {
                get;
                init;
            } = Array.Empty<string>();

            public IReadOnlyList<string> Keywords
            {
                get;
                init;
            } = Array.Empty<string>();

            public string Title { get; init; } =
                string.Empty;

            public string Answer { get; init; } =
                string.Empty;

            public IReadOnlyList<string> Steps
            {
                get;
                init;
            } = Array.Empty<string>();

            public string? ActionText { get; init; }

            public string? ActionPage { get; init; }
        }

        public sealed class ChatbotResponse
        {
            public string Intent { get; init; } =
                string.Empty;

            public string Title { get; init; } =
                string.Empty;

            public string Answer { get; init; } =
                string.Empty;

            public IReadOnlyList<string> Steps
            {
                get;
                init;
            } = Array.Empty<string>();

            public string? ActionText { get; init; }

            public string? ActionPage { get; init; }
        }
    }
}