﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Web.OData.Builder;
using System.Web.OData.Builder.TestModels;
using System.Web.OData.Query.Expressions;
using Microsoft.TestCommon;
using Address = System.Web.OData.Builder.TestModels.Address;
using System.Web.OData.Query;

namespace System.Web.OData.Test.OData.Query
{
    public class ApplyQueryOptionTest
    {
        // Legal apply queries usable against CustomerApplyTestData.
        // Tuple is: apply, expected number
        public static TheoryDataSet<string, List<Dictionary<string, object>>> CustomerTestApplies
        {
            get
            {
                return new TheoryDataSet<string, List<Dictionary<string, object>>>
                {
                    {
                        "aggregate(CustomerId with sum as CustomerId)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "CustomerId", 10} }
                        }
                    },
                    {
                        "aggregate(SharePrice with sum as SharePrice)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "SharePrice", 12.5M} }
                        }
                    },
                    {
                        "aggregate(SharePrice with min as SharePrice)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "SharePrice", 2.5M} }
                        }
                    },
                     {
                        "aggregate(SharePrice with max as SharePrice)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "SharePrice", 10M} }
                        }
                    },
                    {
                        "aggregate(SharePrice with average as SharePrice)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "SharePrice", 6.25M} }
                        }
                    },
                    {
                        "aggregate(SharePrice with average as SharePrice)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "SharePrice", 6.25M} }
                        }
                    },
                    {
                        "aggregate(CustomerId with sum as Total, SharePrice with countdistinct as SharePriceDistinctCount)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "SharePriceDistinctCount", 3L}, { "Total", 10} }
                        }
                    },
                    {
                        "groupby((Name))",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "Name", "Lowest"} },
                            new Dictionary<string, object> { { "Name", "Highest"} },
                            new Dictionary<string, object> { { "Name", "Middle"} }
                        }
                    },
                    {
                        "groupby((Name), aggregate(CustomerId with sum as Total))",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "Name", "Lowest"}, { "Total", 5} },
                            new Dictionary<string, object> { { "Name", "Highest"}, { "Total", 2} },
                            new Dictionary<string, object> { { "Name", "Middle"}, { "Total", 3 } }
                        }
                    },
                    {
                        "filter(Name eq 'Lowest')/groupby((Name))",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "Name", "Lowest"} }
                        }
                    },
                    // TODO: Uncomment after filter will use EdmType generated by groupby
                    //{
                    //    "groupby((Name), aggregate(CustomerId with sum as Total))/filter(Total eq 3)",
                    //    new List<Dictionary<string, object>>
                    //    {
                    //        new Dictionary<string, object> { { "Name", "Middle"}, { "Total", "3" } }
                    //    }
                    //},
                    {
                        "groupby((Name))/filter(Name eq 'Lowest')",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "Name", "Lowest"} }
                        }
                    },
                    {
                        "groupby((Address/City))",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "City", "redmond"} },
                            new Dictionary<string, object> { { "City", "seattle"} },
                            new Dictionary<string, object> { { "City", "hobart"} },
                            new Dictionary<string, object> { { "City", null} },
                        }
                    },
                    {
                        "aggregate(CustomerId mul CustomerId with sum as CustomerId)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "CustomerId", 30} }
                        }
                    },
                    {
                        // Note SharePrice and CustomerId have different type
                        "aggregate(SharePrice mul CustomerId with sum as Result)",
                        new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { "Result", 15.0M} }
                        }
                    },
                };
            }
        }

        // Legal filter queries usable against CustomerFilterTestData.
        // Tuple is: filter, expected list of customer ID's
        public static TheoryDataSet<string, int[]> CustomerTestFilters
        {
            get
            {
                return new TheoryDataSet<string, int[]>
                {
                    // Primitive properties
                    { "Name eq 'Highest'", new int[] { 2 } },
                    { "endswith(Name, 'est')", new int[] { 1, 2, 4 } },

                    // Complex properties
                    { "Address/City eq 'redmond'", new int[] { 1 } },
                    { "contains(Address/City, 'e')", new int[] { 1, 2 } },

                    // Primitive property collections
                    { "Aliases/any(alias: alias eq 'alias34')", new int[] { 3, 4 } },
                    { "Aliases/any(alias: alias eq 'alias4')", new int[] { 4 } },
                    { "Aliases/all(alias: alias eq 'alias2')", new int[] { 2 } },

                    // Navigational properties
                    { "Orders/any(order: order/OrderId eq 12)", new int[] { 1 } },
                };
            }
        }

        // Test data used by CustomerTestApplies TheoryDataSet
        public static List<Customer> CustomerApplyTestData
        {
            get
            {
                List<Customer> customerList = new List<Customer>();

                Customer c = new Customer
                {
                    CustomerId = 1,
                    Name = "Lowest",
                    SharePrice = 10,
                    Address = new Address { City = "redmond" },
                };
                c.Orders = new List<Order>
                {
                    new Order { OrderId = 11, Customer = c },
                    new Order { OrderId = 12, Customer = c },
                };
                customerList.Add(c);

                c = new Customer
                {
                    CustomerId = 2,
                    Name = "Highest",
                    SharePrice = 2.5M,
                    Address = new Address { City = "seattle" },
                    Aliases = new List<string> { "alias2", "alias2" }
                };
                customerList.Add(c);

                c = new Customer
                {
                    CustomerId = 3,
                    Name = "Middle",
                    Address = new Address { City = "hobart" },
                    Aliases = new List<string> { "alias2", "alias34", "alias31" }
                };
                customerList.Add(c);

                c = new Customer
                {
                    CustomerId = 4,
                    Name = "Lowest",
                    Aliases = new List<string> { "alias34", "alias4" }
                };
                customerList.Add(c);

                return customerList;
            }
        }

        [Theory]
        [PropertyData("CustomerTestApplies")]
        public void ApplyTo_Returns_Correct_Queryable(string filter, List<Dictionary<string, object>> aggregation)
        {
            // Arrange
            var model = new ODataModelBuilder()
                            .Add_Order_EntityType()
                            .Add_Customer_EntityType_With_Address()
                            .Add_CustomerOrders_Relationship()
                            .Add_Customer_EntityType_With_CollectionProperties()
                            .Add_Customers_EntitySet()
                            .GetEdmModel();
            var context = new ODataQueryContext(model, typeof(Customer));
            var applyOption = new ApplyQueryOption(filter, context);
            IEnumerable<Customer> customers = CustomerApplyTestData;

            // Act
            IQueryable queryable = applyOption.ApplyTo(customers.AsQueryable(), new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.True });


            // Assert
            Assert.NotNull(queryable);
            var actualCustomers = Assert.IsAssignableFrom<IEnumerable<DynamicTypeWrapper>>(queryable).ToList();

            Assert.Equal(aggregation.Count(), actualCustomers.Count());

            var aggEnum = actualCustomers.GetEnumerator();

            foreach (var expected in aggregation)
            {
                aggEnum.MoveNext();
                var agg = aggEnum.Current;
                foreach (var key in expected.Keys)
                {
                    object value;
                    Assert.True(agg.TryGetPropertyValue(key, out value), "Property " + key + " not found");
                    Assert.Equal(expected[key], value);
                }

            }
        }

        [Theory]
        [PropertyData("CustomerTestFilters")]
        public void ApplyTo_Returns_Correct_Queryable_ForFilter(string filter, int[] customerIds)
        {
            // Arrange
            var model = new ODataModelBuilder()
                            .Add_Order_EntityType()
                            .Add_Customer_EntityType_With_Address()
                            .Add_CustomerOrders_Relationship()
                            .Add_Customer_EntityType_With_CollectionProperties()
                            .Add_Customers_EntitySet()
                            .GetEdmModel();
            var context = new ODataQueryContext(model, typeof(Customer));
            var filterOption = new ApplyQueryOption(string.Format("filter({0})", filter), context);
            IEnumerable<Customer> customers = CustomerApplyTestData;

            // Act
            IQueryable queryable = filterOption.ApplyTo(customers.AsQueryable(), new ODataQuerySettings { HandleNullPropagation = HandleNullPropagationOption.True });

            // Assert
            Assert.NotNull(queryable);
            IEnumerable<Customer> actualCustomers = Assert.IsAssignableFrom<IEnumerable<Customer>>(queryable);
            Assert.Equal(
                customerIds,
                actualCustomers.Select(customer => customer.CustomerId));
        }
    }
}