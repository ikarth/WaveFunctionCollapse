﻿/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;


static class Program
{
    static void Benchmark(Model model)
    {
        Console.WriteLine($"Total wall time: {model.benchmarker.Elapsed.TotalMilliseconds} ms");

        var observation_times = model.observation_begin.Zip(model.observation_end, (car, cdr) => cdr - car);
        double total_observation = observation_times.Aggregate<double>((accum, x) => accum + x);
        Console.WriteLine($"Observation time: {total_observation} ms, across {model.observation_begin.Count} observations");

        var propagation_times = model.propagation_begin.Zip(model.propagation_end, (car, cdr) => cdr - car);
        double total_propagation = propagation_times.Aggregate<double>((accum, x) => accum + x);
        Console.WriteLine($"Propagation time: {total_propagation} ms, across {model.propagation_begin.Count} propagations");

        var total_run = model.run_end - model.run_begin;
        Console.WriteLine($"Generation time: {total_run} ms");

        double pattern_time = model.pattern_extraction_end - model.pattern_extraction_begin;
        Console.WriteLine($"Pattern extraction time: {pattern_time} (includes the creation of propagator[])");

        Console.WriteLine($"Patterns: {model.BenchmarkPatternCount()}");
        Console.WriteLine($"Adjacencies: {model.BenchmarkAdjacencyCount()}");
        Console.WriteLine($"Output Size: {model.BenchmarkOuputSizeX()} x {model.BenchmarkOuputSizeY()}");
        Console.WriteLine($"Pattern Size: {model.BenchmarkPatternSize()}");
        Console.WriteLine($"------------------");
        Console.WriteLine($"");
    }

    static void BenchmarkSpreadsheet(Model model)
    {
        Console.Write($"{model.benchmarker.Elapsed.TotalMilliseconds - model.run_begin},");
        var observation_times = model.observation_begin.Zip(model.observation_end, (car, cdr) => cdr - car);
        double total_observation = observation_times.Aggregate<double>((accum, x) => accum + x);
        var propagation_times = model.propagation_begin.Zip(model.propagation_end, (car, cdr) => cdr - car);
        double total_propagation = propagation_times.Aggregate<double>((accum, x) => accum + x);
        var total_run = model.run_end - model.run_begin;
        double pattern_time = model.pattern_extraction_end - model.pattern_extraction_begin;
        Console.Write($"{total_observation}, {model.observation_begin.Count}, {total_propagation}, {model.propagation_begin.Count}, {total_run}, {pattern_time}, {model.BenchmarkPatternCount()},{model.BenchmarkAdjacencyCount()}, {model.BenchmarkOuputSizeX()} x {model.BenchmarkOuputSizeY()},{model.BenchmarkPatternSize()}\n");
    }
	static void Main()
	{
		Stopwatch sw = Stopwatch.StartNew();

		Random random = new Random();
		XDocument xdoc = XDocument.Load("samples.xml");
        Console.WriteLine("Name, Completion, Total Wall Time Elapsed (in ms), Observation Time, Observation Count, Propagation Time, Propagation Count, Generation Run Time, Pattern Extraction Time, Pattern Count, Adjacencies, Output Size, Pattern Size");
		int counter = 1;
		foreach (XElement xelem in xdoc.Root.Elements("overlapping", "simpletiled"))
		{
			Model model;
			string name = xelem.Get<string>("name");
			

			if (xelem.Name == "overlapping") model = new OverlappingModel(name, xelem.Get("N", 2), xelem.Get("width", 48), xelem.Get("height", 48), 
				xelem.Get("periodicInput", true), xelem.Get("periodic", false), xelem.Get("symmetry", 8), xelem.Get("ground", 0));
			else if (xelem.Name == "simpletiled") model = new SimpleTiledModel(name, xelem.Get<string>("subset"), 
				xelem.Get("width", 10), xelem.Get("height", 10), xelem.Get("periodic", false), xelem.Get("black", false));
			else continue;

			for (int i = 0; i < xelem.Get("screenshots", 2); i++)
			{
				for (int k = 0; k < 10; k++)
				{
                    Console.Write($"{name},");
                    //Console.Write("> ");
                    int seed = random.Next();
					bool finished = model.Run(seed, xelem.Get("limit", 0));
                    if (finished)
                    {
                        Console.Write("DONE,");

                        model.Graphics().Save($"{counter} {name} {i}.png");
                        if (model is SimpleTiledModel && xelem.Get("textOutput", false))
                            System.IO.File.WriteAllText($"{counter} {name} {i}.txt", (model as SimpleTiledModel).TextOutput());

                        BenchmarkSpreadsheet(model);

                        break;
                    }
                    else
                    {
                        Console.Write("CONTRADICTION,");
                        BenchmarkSpreadsheet(model);
                    }
				}
                

			}

			counter++;
		}

		Console.WriteLine($"time = {sw.ElapsedMilliseconds}");
	}
}
