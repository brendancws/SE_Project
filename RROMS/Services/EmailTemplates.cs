namespace RROMS.Services
{
    public static class EmailTemplates
    {
        public static string Wrap(
            string title,
            string content)
        {
            return $"""
            <!DOCTYPE html>
            <html>
            <body style="
                margin:0;
                padding:24px;
                background:#f3f4f6;
                font-family:'Segoe UI',Arial,sans-serif;">

                <div style="
                    max-width:620px;
                    margin:auto;
                    background:#ffffff;
                    border:1px solid #e5e7eb;
                    border-radius:16px;
                    overflow:hidden;">

                    <div style="
                        background:#f59e0b;
                        padding:24px;">

                        <h1 style="
            margin:0;
            color:white;
            font-size:32px;
            font-weight:800;">
                            Dineflow
                        </h1>

                        <p style="
                            margin:8px 0 0;
                            color:#fef3c7;">
                            Restaurant Reservation & Ordering
                        </p>
                    </div>

                    <div style="padding:32px;">

                        <h2 style="
                            margin-top:0;
                            color:#111827;">
                            {title}
                        </h2>

                        {content}

                    </div>

                    <div style="
                        padding:18px 32px;
                        border-top:1px solid #e5e7eb;
                        background:#f9fafb;
                        color:#6b7280;
                        font-size:13px;">

                        This is an automated message from Dineflow.
            Please do not reply directly to this email.

            © Dineflow Restaurant Reservation & Ordering System

                    </div>
                </div>
            </body>
            </html>
            """;
        }

        public static string PaymentReceipt(
    string customerName,
    int orderId,
    decimal amount,
    string transactionNo,
    DateTime paymentDate)
        {
            return Wrap(
                "Payment Receipt",
                $"""
        <p>Hello {customerName},</p>

        <p>
            Your payment has been received successfully.
            Below are your payment details.
        </p>

        <div style="
            background:#f9fafb;
            border-radius:12px;
            padding:18px;
            margin:20px 0;">

            <strong>Order Number</strong><br>
            ORD-{orderId:D6}

        </div>

        <div style="
            background:#f9fafb;
            border-radius:12px;
            padding:18px;
            margin:14px 0;">

            <strong>Transaction Number</strong><br>
            {transactionNo}

            <br><br>

            <strong>Payment Date</strong><br>
            {paymentDate:dd MMM yyyy hh:mm tt}

            <br><br>

            <strong>Payment Method</strong><br>
            Card

            <br><br>

            <strong>Status</strong><br>
            Paid

            <br><br>

            <strong>Amount Paid</strong><br>
            RM {amount:0.00}
        </div>

        <p>
            Thank you for choosing Dineflow.
        </p>

        <p>
            Regards,<br>
            Dineflow Payments Team
        </p>
        """);
        }

        public static string OrderConfirmationPendingPayment(
    string customerName,
    int orderId,
    decimal amount)
        {
            return Wrap(
                "Order Confirmation",
                $"""
        <p>Hello {customerName},</p>

        <p>
            Your food order has been placed successfully.
        </p>

        <div style="
            background:#f9fafb;
            border-radius:12px;
            padding:18px;
            margin:20px 0;">

            <strong>Order Number</strong><br>
            ORD-{orderId:D6}

        </div>

        <div style="
            background:#f9fafb;
            border-radius:12px;
            padding:18px;
            margin:14px 0;">

            <strong>Payment Method</strong><br>
            Cash on Arrival

            <br><br>

            <strong>Payment Status</strong><br>
            Pending

            <br><br>

            <strong>Amount Due</strong><br>
            RM {amount:0.00}

        </div>

        <p>
            Please make payment at the restaurant when you arrive.
        </p>

        <p>
            Regards,<br>
            Dineflow Team
        </p>
        """);
        }
    }

}