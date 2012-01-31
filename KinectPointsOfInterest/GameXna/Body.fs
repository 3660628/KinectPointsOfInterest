namespace BodyData
    exception InvalidJointNameException of string

    open Microsoft.Xna.Framework
    open Microsoft.Xna.Framework.Graphics
    
    [<AllowNullLiteral>] //allow null as a proper value
    type Body(hea, shL, shR, shC, hiL, hiR, hiC, foL, foR, knL, knR)=
           
        //the depth image of the player.  it should only contain one player and each depth should be an int from  850 - 4000 (the valid depths for the depths)  
        let mutable depthImage:int[] = Array.create 76800 0

        //joints
        let mutable head = hea
        let mutable shoulderL = shL
        let mutable shoulderR = shR
        let mutable shoulderC = shC
        let mutable hipL = hiL
        let mutable hipR = hiR
        let mutable hipC = hiC
        let mutable footL = foL
        let mutable footR = foR
        let mutable kneeL = knL
        let mutable kneeR = knR

        new() = new Body(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 0.0f))

        member this.SetSkeleton (hea, shL, shR, shC, hiL, hiR, hiC, foL, foR, knL, knR)=
             head <- hea
             shoulderL <- shL
             shoulderR <- shR
             shoulderC <- shC
             hipL <- hiL
             hipR <- hiR
             hipC <- hiC
             footL <- foL
             footR <- foR
             kneeL <- knL
             kneeR <- knR

        member this.DepthImg
            with set(dI) = depthImage <- dI
            and get() = depthImage

        member this.GetJoint jointName=
            match jointName with
            | "head" -> head:Vector3
            | "leftShoulder" -> shoulderL:Vector3
            | "rightShoulder" -> shoulderR:Vector3
            | "centerShoulder" -> shoulderC:Vector3
            | "leftHip" -> hipL:Vector3
            | "rightHip" -> hipR:Vector3
            | "centerHip" -> hipC:Vector3
            | "leftFoot" -> footL:Vector3
            | "rightFoot" -> footR:Vector3
            | "leftKnee" -> kneeL:Vector3
            | "rightKnee" -> kneeR:Vector3
            | _ -> raise (InvalidJointNameException("Cannot recognise joint '" + jointName + "'")) 

        member this.GetSkeleton=
            (head, shoulderL, shoulderR, shoulderC, hipL, hipR, hipC, footL, footR, kneeL, kneeR) 

        member this.CompleteBody =  //complete body if has joints and depth data
            if Array.max(depthImage) > 0 then
                if not(head.Equals(new Vector3(0.0f,0.0f,0.0f))) then
                    true
                else
                    false
            else
                false

    