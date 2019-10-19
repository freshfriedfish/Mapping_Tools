﻿using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Generators {
    public class IntersectionGenerator : RelevantObjectsGenerator {
        public override string Name => "Intersection Point Calculator";
        public override string Tooltip => "Takes a pair of virtual lines or circles and generates a virtual point on each of their intersections.";
        public override GeneratorType GeneratorType => GeneratorType.Geometries;

        [RelevantObjectsGeneratorMethod]
        public RelevantPoint GetLineLineIntersection(RelevantLine line1, RelevantLine line2) {
            return Line2.Intersection(line1.Child, line2.Child, out var intersection) ? new RelevantPoint(intersection) : null;
        }

        [RelevantObjectsGeneratorMethod]
        public IEnumerable<RelevantPoint> GetLineCircleIntersection(RelevantLine line, RelevantCircle circle) {
            return Circle.Intersection(circle.Child, line.Child, out var intersections) ? intersections.Select(o => new RelevantPoint(o)) : null;
        }

        [RelevantObjectsGeneratorMethod]
        public IEnumerable<RelevantPoint> GetCircleCircleIntersection(RelevantCircle circle1, RelevantCircle circle2) {
            return Circle.Intersection(circle1.Child, circle2.Child, out var intersections) ? intersections.Select(o => new RelevantPoint(o)) : null;
        }
    }
}
