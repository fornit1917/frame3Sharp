﻿using System;
using UnityEngine;
using g3;

namespace f3
{
	//
	// this Widget implements translation constrained to an axis
	// 
	public class AxisTranslationWidget : Standard3DTransformWidget
    {
        // scaling distance along axis multiplied by this value. 
        // By default click-point stays under cursor (ie direct manipulation), but 
        // you can slow it down by setting this to return a smaller value.
        // Also you can use this to compensate for global scene scaling (hence the function)
        public Func<float> TranslationScaleF = () => { return 1.0f; };


        public Func<Frame3f, int, float, float> DeltaDistanceConstraintF = null;


        int nTranslationAxis;

		public AxisTranslationWidget(int nFrameAxis)
		{
			nTranslationAxis = nFrameAxis;
		}

		// stored frames from target used during click-drag interaction
		Frame3f translateFrameL;		// local-spaace frame
		Frame3f translateFrameW;		// world-space frame
		Vector3f translateAxisW;		// world translation axis (redundant...)

		// computed values during interaction
		Frame3f raycastFrame;		// camera-facing plane containing translateAxisW
		float fTranslateStartT;		// start T-value along translateAxisW

		public override bool BeginCapture(ITransformable target, Ray3f worldRay, UIRayHit hit)
		{
			// save local and world frames
			translateFrameL = target.GetLocalFrame (CoordSpace.ObjectCoords);
			translateFrameW = target.GetLocalFrame (CoordSpace.WorldCoords);
			translateAxisW = translateFrameW.GetAxis (nTranslationAxis);

			// save t-value of closest point on translation axis, so we can find delta-t
			Vector3f vWorldHitPos = hit.hitPos;
			fTranslateStartT = Distance.ClosestPointOnLineT(
				translateFrameW.Origin, translateAxisW, vWorldHitPos);

            // construct plane we will ray-intersect with in UpdateCapture()
            Vector3f makeUp = Vector3f.Cross(FPlatform.MainCamera.Forward(), translateAxisW).Normalized;
            Vector3f vPlaneNormal = Vector3f.Cross(makeUp, translateAxisW).Normalized;
            raycastFrame = new Frame3f(vWorldHitPos, vPlaneNormal);

            return true;
		}

		public override bool UpdateCapture(ITransformable target, Ray3f worldRay)
		{
			// ray-hit with plane that contains translation axis
			Vector3f planeHit = raycastFrame.RayPlaneIntersection(worldRay.Origin, worldRay.Direction, 2);

			// figure out new T-value along axis, then our translation update is delta-t
			float fNewT = Distance.ClosestPointOnLineT (translateFrameW.Origin, translateAxisW, planeHit);
			float fDeltaT = (fNewT - fTranslateStartT);
            fDeltaT *= TranslationScaleF();
            if (DeltaDistanceConstraintF != null)
                fDeltaT = DeltaDistanceConstraintF(translateFrameL, nTranslationAxis, fDeltaT);

            // construct new frame translated along axis (in local space)
            Frame3f newFrame = translateFrameL;
			newFrame.Origin += fDeltaT * translateFrameL.GetAxis(nTranslationAxis);

			// update target
			target.SetLocalFrame (newFrame, CoordSpace.ObjectCoords);

			return true;
		}

        public override bool EndCapture(ITransformable target)
        {
            return true;
        }

        public override void Disconnect()
        {
            RootGameObject.Destroy();
        }


        static Interval1d CosVisibilityRange = new Interval1d(
            -Math.Cos(45 * MathUtil.Deg2Rad), Math.Cos(15 * MathUtil.Deg2Rad));
        public override bool CheckVisibility(ref Frame3f curFrameW, ref Vector3d eyePosW) {
            Vector3d axis = curFrameW.GetAxis(nTranslationAxis);
            Vector3d eyevec = (eyePosW - curFrameW.Origin).Normalized;
            double dot = axis.Dot(eyevec);
            return CosVisibilityRange.Contains(dot);
        }

    }
}

