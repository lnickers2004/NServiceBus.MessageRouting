﻿using System;
using System.Linq;
using NServiceBus.MessageRouting.RoutingSlips;
using NUnit.Framework;
using Should;

namespace NServiceBus.MessageRouting.UnitTests.RoutingSlips
{
    [TestFixture]
    public class RouterTests 
    {
        private class DummyMessage : IMessage
        {
            
        }

        [Test]
        public void Should_build_routing_slip()
        {
            var builder = new RoutingSlipBuilder();

            var routingSlipId = Guid.NewGuid();

            RoutingSlip routingSlip = builder.CreateRoutingSlip(routingSlipId, "foo");

            routingSlip.ShouldNotBeNull();
            routingSlip.Id.ShouldEqual(routingSlipId);
            routingSlip.Itinerary.Count().ShouldEqual(1);
            routingSlip.Itinerary.First().Address.ShouldEqual("foo");
        }

        [Test]
        public void Should_send_to_first_destination()
        {
            var routingSlip = new RoutingSlipBuilder().CreateRoutingSlip(Guid.NewGuid(), "foo");

            var bus = new Bus();
            var router = new Router(bus);

            var message = new DummyMessage();
            router.SendToFirstStep(message, routingSlip);

            bus.OutgoingHeaders[Router.RoutingSlipHeaderKey].ShouldNotBeNull();
            bus.ExplicitlySent.Count().ShouldEqual(1);
            bus.ExplicitlySent.First().Item1.ShouldEqual("foo");
            bus.ExplicitlySent.First().Item2.ShouldEqual(message);
        }

        [Test]
        public void Should_send_to_next_destination_if_no_error()
        {
            var routingSlip = new RoutingSlipBuilder().CreateRoutingSlip(Guid.NewGuid(), "foo", "bar");

            var bus = new Bus();
            var router = new Router(bus);

            router.SendToNextStep(null, routingSlip);

            bus.CurrentMessageContext.Headers[Router.RoutingSlipHeaderKey].ShouldNotBeNull();

            routingSlip.Itinerary.Count.ShouldEqual(1);
            routingSlip.Log.Count.ShouldEqual(1);
            routingSlip.Log[0].Address.ShouldEqual("foo");

            bus.Forwarded.Count().ShouldEqual(1);
            bus.Forwarded.ShouldContain("bar");
        }

        [Test]
        public void Should_complete_route()
        {
            var routingSlip = new RoutingSlipBuilder().CreateRoutingSlip(Guid.NewGuid(), "foo", "bar");

            var bus = new Bus();
            var router = new Router(bus);

            router.SendToNextStep(null, routingSlip);
            
            bus.CurrentMessageContext.Headers.Clear();
            
            router.SendToNextStep(null, routingSlip);

            bus.CurrentMessageContext.Headers.ContainsKey(Router.RoutingSlipHeaderKey).ShouldBeFalse();

            routingSlip.Itinerary.Count.ShouldEqual(0);
            routingSlip.Log.Count.ShouldEqual(2);
            routingSlip.Log[0].Address.ShouldEqual("foo");

            bus.Forwarded.Count().ShouldEqual(1);

        }


        [Test]
        public void Should_not_send_to_next_destination_if_error()
        {
            var routingSlip = new RoutingSlipBuilder().CreateRoutingSlip(Guid.NewGuid(), "foo", "bar");

            var bus = new Bus();
            var router = new Router(bus);

            router.SendToNextStep(new Exception(), routingSlip);

            bus.CurrentMessageContext.Headers.ContainsKey(Router.RoutingSlipHeaderKey).ShouldBeFalse();

            routingSlip.Itinerary.Count.ShouldEqual(2);
            routingSlip.Log.Count.ShouldEqual(0);

            bus.Forwarded.Count().ShouldEqual(0);
        }
    }
}