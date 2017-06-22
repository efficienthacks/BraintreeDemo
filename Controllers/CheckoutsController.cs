using Braintree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BraintreeASPExample.Controllers
{
    public class CheckoutsController : Controller
    {
        public IBraintreeConfiguration config = new BraintreeConfiguration();

        public static readonly TransactionStatus[] transactionSuccessStatuses = {
                                                                                    TransactionStatus.AUTHORIZED,
                                                                                    TransactionStatus.AUTHORIZING,
                                                                                    TransactionStatus.SETTLED,
                                                                                    TransactionStatus.SETTLING,
                                                                                    TransactionStatus.SETTLEMENT_CONFIRMED,
                                                                                    TransactionStatus.SETTLEMENT_PENDING,
                                                                                    TransactionStatus.SUBMITTED_FOR_SETTLEMENT,
                                                                                    
                                                                                };
        public static readonly SubscriptionStatus[] subSuccessStatuses = {
            SubscriptionStatus.ACTIVE, SubscriptionStatus.PENDING
        };

        public ActionResult New()
        {
            var gateway = config.GetGateway();
            var clientToken = gateway.ClientToken.generate();
            ViewBag.ClientToken = clientToken;
            return View();
        }

        public ActionResult CheckoutPage()
        {
            return View();
        }

        public ActionResult Create()
        {
            var gateway = config.GetGateway();
            /*Decimal amount;

            try
            {
                amount = Convert.ToDecimal(Request["amount"]);
            }
            catch (FormatException e)
            {
                TempData["Flash"] = "Error: 81503: Amount is an invalid format.";
                return RedirectToAction("New");
            }*/

            string subscriptionPlanId = "hnmb";

            //option 1: check database for the customer id.
            string customerId = "837767049";

            //option 2: find the customer in braintree
            //var customers = gateway.Customer.Search(new CustomerSearchRequest().Email.Is("theEmailAddr"));
            //var customerResult = customers.FirstItem;

            if (customerId == "")
            {
                //if cust id doesn't exist:
                var customerrequest = new CustomerRequest
                {
                    FirstName = "Mark",
                    LastName = "Jones",
                    Company = "Jones Co.",
                    Email = "mark.jones@example.com",
                    Fax = "419-555-1234",
                    Phone = "614-555-1234",
                    Website = "http://example.com"
                };
                Result<Customer> custresult = gateway.Customer.Create(customerrequest);

                bool success = custresult.IsSuccess();
                // true
                customerId = custresult.Target.Id;
            }

            //check whether they already have an active subscription
            //option 1: get subscription id from your database
            //option 2: get subscription id from Braintree
            var customer = gateway.Customer.Find(customerId);
            var paymentMethods = customer.PaymentMethods;
            var subscriptionsWithPlan = new List<string>();
            foreach (dynamic pm in paymentMethods)
            {
                try
                {
                    Subscription[] subs = pm.Subscriptions;

                    foreach (var sub in subs)
                    {
                        if (sub.PlanId == subscriptionPlanId && subSuccessStatuses.Contains(sub.Status))
                        {
                            //add to list
                            Console.WriteLine("customer has an active subscription");
                            subscriptionsWithPlan.Add(sub.Id);
                        }
                    }
                }
                catch(Exception e)
                {
                    int i = 0;
                }
            }
            
            if(subscriptionsWithPlan.Count() > 0)
            {
                return RedirectToAction("New");
            }


            //do the subscription
            var nonce = Request["payment_method_nonce"];

            var pmrequest = new PaymentMethodRequest
            {
                CustomerId = customerId,
                PaymentMethodNonce = nonce
            };

            Result<PaymentMethod> pmresult = gateway.PaymentMethod.Create(pmrequest);

            var subsrequest = new SubscriptionRequest
            {
                PaymentMethodToken = pmresult.Target.Token,
                PlanId = subscriptionPlanId
            };
            Result<Subscription> subsresult = gateway.Subscription.Create(subsrequest);

            if (subsresult.IsSuccess())
            {
                Subscription subs = subsresult.Target;
                return RedirectToAction("Show", new { id = subs.Id, type = "subscription" });
            }
            else if (subsresult.Subscription != null)
            {
                //what scenario is this?
                return RedirectToAction("Show", new { id = subsresult.Subscription.Id, type = "subscription" });
            }
            else
            {
                string errorMessages = "";
                foreach (ValidationError error in subsresult.Errors.DeepAll())
                {
                    errorMessages += "Error: " + (int)error.Code + " - " + error.Message + "\n";
                }
                TempData["Flash"] = errorMessages;
                return RedirectToAction("New");
            }
            /*var request = new TransactionRequest
            {
                Amount = amount,
                PaymentMethodNonce = nonce,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };
            Result<Transaction> result = gateway.Transaction.Sale(request);
            
            if (subsresult.IsSuccess())
            {
                Transaction transaction = result.Target;
                return RedirectToAction("Show", new { id = transaction.Id });
            }
            else if (result.Transaction != null)
            {
                return RedirectToAction("Show", new { id = result.Transaction.Id } );
            }
            else
            {
                string errorMessages = "";
                foreach (ValidationError error in result.Errors.DeepAll())
                {
                    errorMessages += "Error: " + (int)error.Code + " - " + error.Message + "\n";
                }
                TempData["Flash"] = errorMessages;
                return RedirectToAction("New");
            }*/
        }

        public ActionResult Show(String id, string type = "transaction")
        {
            var gateway = config.GetGateway();
            if (type == "subscription")
            {
                var subs = gateway.Subscription.Find(id);

                if (subSuccessStatuses.Contains(subs.Status))
                {
                    TempData["header"] = "Sweet Success!";
                    TempData["icon"] = "success";
                    TempData["message"] = "Your test transaction has been successfully processed. See the Braintree API response and try again.";
                }
                else
                {
                    TempData["header"] = "Transaction Failed";
                    TempData["icon"] = "fail";
                    TempData["message"] = "Your test transaction has a status of " + subs.Status + ". See the Braintree API response and try again.";
                };

                ViewBag.Transaction = subs.Transactions.FindLast(x=>x.Recurring == true);
            }
            if (type == "transaction")
            {
                Transaction transaction = gateway.Transaction.Find(id);

                if (transactionSuccessStatuses.Contains(transaction.Status))
                {
                    TempData["header"] = "Sweet Success!";
                    TempData["icon"] = "success";
                    TempData["message"] = "Your test transaction has been successfully processed. See the Braintree API response and try again.";
                }
                else
                {
                    TempData["header"] = "Transaction Failed";
                    TempData["icon"] = "fail";
                    TempData["message"] = "Your test transaction has a status of " + transaction.Status + ". See the Braintree API response and try again.";
                };

                ViewBag.Transaction = transaction;
                
            }
            return View();
        }
    }
}
