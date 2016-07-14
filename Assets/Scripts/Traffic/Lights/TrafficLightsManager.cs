// Copyright (C) 2016 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: a TrafficLightManager defines a program with different sequences,
//          and each sequence has multiple states and durations for each light.

using UnityEngine;
using LightActivation = TrafficLight.LightActivation;

public class TrafficLightsManager : MonoBehaviour
{

    [System.Serializable]
    public class Program
    {
        public string name;
        public float duration;
        public float offset;
        public Sequence[] sequences;
    }

    [System.Serializable]
    public class Sequence
    {
        public string name;
        public float offset;
        public State[] states;
        public TrafficLight[] lights;
        [HideInInspector] public State currentState = null;
    }

    [System.Serializable]
    public class State
    {
        public string name;
        public float duration;
        public LightActivation[] activeLights;
    }


    public TimeController timeController = null;
    public Program[] programs = defaultProgram();

    private int currentProgram = 0;

    private static Program[] defaultProgram()
    {
        return new Program[] {
            new Program() {
                name = "default",
                duration = 5,
                sequences = new Sequence[] {
                    new Sequence() {
                        name = "NorthSouth Vehicles",
                        states = new State[] {
                            new State() {
                                name = "Red",
                                duration = 3,
                                activeLights = new LightActivation[] { new LightActivation { id = 1 } }
                            },
                            new State() {
                                name = "Green",
                                duration = 3,
                                activeLights = new LightActivation[] { new LightActivation { id = 3 } }
                            },
                            new State() {
                                name = "Yellow",
                                duration = 1,
                                activeLights = new LightActivation[] { new LightActivation { id = 2 } }
                            }
                        }
                    },
                    new Sequence() {
                        name = "EastWest Pedestrians",
                        states = new State[] {
                            new State() {
                                name = "Red",
                                duration = 1,
                                activeLights = new LightActivation[] { new LightActivation { id = 1 } }
                            },
                            new State() {
                                name = "Green",
                                duration = 1,
                                activeLights = new LightActivation[] { new LightActivation { id = 2 } }
                            },
                        }
                    }

                }
            }
        };
    }

    void Update()
    {
        UpdateTrafficLights(timeController.time);
    }

    void UpdateTrafficLights(float time)
    {
        Program program = programs[currentProgram];
        float timeInProgram = (time + program.duration - program.offset) % program.duration;

        for (int i = program.sequences.Length -1; i >= 0 ; i--)
        {
            var sequence = program.sequences[i];
            if (sequence.lights.Length > 0 && sequence.states.Length > 0)
            {
                float timeInSequence = (timeInProgram + program.duration - sequence.offset) % program.duration;

                // Show first state by default (or if sequence has already finished)
                State newState = sequence.states[0];

                // Find which state of the sequence it should show
                for (int j = 0; j < sequence.states.Length; j++)
                {
                    var state = sequence.states[j];
                    timeInSequence -= state.duration;
                    if (timeInSequence < 0)
                    {
                        newState = state;
                        break;
                    }
                }

                if (sequence.currentState != newState)
                {
                    sequence.currentState = newState;

                    // Update traffic lights with currently active light Ids
                    for (int j = sequence.lights.Length - 1; j >= 0; j--)
                    {
                        sequence.lights[j].ChangeLights(newState.activeLights);
                    }
                }
            }
        }
    }

}
