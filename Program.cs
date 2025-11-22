// See https://aka.ms/new-console-template for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Program {
	public static void Main(string[] args){
		Console.WriteLine("SimpleScript v1.0");
		Macros macrosing = new Macros();
		Stack stacky = new Stack();

		for(int i = 0; i < args.Length; ++i) {
			switch(args[i]){
				case "-f":
					++i;
					stacky.push(args[i], 0);
					string asm_text = "[BITS 16]\n";
					while(!stacky.isEmpty()){
						INCLUDED:
						Console.WriteLine($"Importing ./{stacky.peek().file}");
						string text = File.ReadAllText($"./{stacky.peek().file}");
						Console.WriteLine(text);
						string[] instructions = text.Split(";");
						for(int a = stacky.peek().startAt; a < instructions.Length; ++a){
							Console.WriteLine($"Trying compile {instructions[a]}");
							instructions[a] = instructions[a].Trim();

							if(instructions[a].IndexOf("//") != -1){
								continue;
							}

							if(instructions[a].IndexOf("#define") != -1){
								string[] macro_text = instructions[a].Substring(8, instructions[a].Length-10).Trim().Split("asm(\"");
								macrosing.addMacro(macro_text);
							} else if(instructions[a].IndexOf("asm") != -1){
								int fquote = instructions[a].IndexOf("\"");
								int lquote = instructions[a].IndexOf("\"", fquote + 1);
								string assembled = instructions[a].Substring(fquote+1, lquote-fquote-1);
								asm_text += assembled;
								asm_text += "\n";
							} else if(instructions[a].IndexOf("#include") != -1){
								Console.WriteLine("Loading include");
								int fquote = instructions[a].IndexOf("\"");
								int lquote = instructions[a].IndexOf("\"", fquote + 1);
								string way = instructions[a].Substring(fquote+1, lquote-fquote-1);
								stacky.update();
								stacky.push(way, 0);
								Console.WriteLine(stacky.peek().file);
								goto INCLUDED;
							} else if(instructions[a] != "") {
								Macro temp = macrosing.startMacro;
								int paramCount = 0;
								Macro bestMacro = macrosing.startMacro;
								while(temp != null){
									bool isAs = true;
									int checkAt = 0;
									// Console.WriteLine(temp.nm.Length);
									for(int b = 0; b < temp.nm.Length; ++b){
										// Console.WriteLine(instructions[a]);
										// Console.WriteLine(temp.nm[b]);
										// Console.WriteLine(checkAt);
										checkAt = instructions[a].IndexOf(temp.nm[b], checkAt);
										if(checkAt == -1){
											// Console.WriteLine("Incomp");
											isAs = false;
											break;
										}
									}
									if(isAs){
										if(paramCount < temp.nm.Length){
											bestMacro = temp;
										}
										break;
									}
									// Console.WriteLine(temp.nextMacro);
									temp = temp.nextMacro;
								}
								// Console.WriteLine(bestMacro.mt);
								asm_text += macrosing.ImplementMacro(bestMacro, instructions[a]);
								asm_text += "\n";
							}
							Console.WriteLine($"Success: {instructions[a]}\n");
							stacky.update();
						}
						stacky.pop();
					}

					asm_text += "times 510-($-$$) db 0\ndw 0AA55h";
					FileStream fl = File.Create("build.asm");
					fl.Close();
					File.WriteAllText("build.asm", asm_text);
					System.Diagnostics.Process.Start("C:/PROGRA~1/NASM/nasm.exe", "-f bin build.asm -o build.bin");
					System.Diagnostics.Process.Start("C:/PROGRA~1/qemu/qemu-system-x86_64", "-drive format=raw,file=build.bin"); 
					break;

			}
		}

	}
}

public class StackNode {
	public string file;
	public StackNode prevNode;
	public int startAt;

	public StackNode(string f, int s){
		file = f;
		startAt = s;
	}
}

public class Stack {
	public StackNode last; 
	public Stack(){}

	public void push(string f, int s){
		StackNode node = new StackNode(f, s);
		if(last != null) node.prevNode = last;
		last = node;
	}	

	public void pop(){
		if(last.prevNode != null) last = last.prevNode;
		else last = null;
	}

	public StackNode peek(){
		return last;
	}

	public void update(){
		++last.startAt;
	}

	public bool isEmpty(){
		return (last == null);
	}
}

public class Macro {
	public string mt, at; 
	public string[] nm;
	public Macro nextMacro;
	public bool hard;

	public Macro(){
		mt = "";
		at = "";
	}
}

public class Macros {
	public Macro startMacro;

	public Macros(){}

	public void addMacro(string[] macros){
		if(startMacro == null){
			int name = macros[0].IndexOf(" ");
			startMacro = new Macro();
			startMacro.nm = macros[0].Substring(0, name).Replace("%a", " ").Replace("%b", " ").Replace("%c", " ").Replace("%d", " ").Trim().Split(" ");
			for(int p = 0; p < startMacro.nm.Length; ++p) Console.WriteLine(startMacro.nm[p]);
			startMacro.mt = macros[0];
			string mm = macros[1];
			for(int i = 2; i < macros.Length; ++i){
				mm += "asm(\"";
				mm += macros[i];
				startMacro.hard = true;
			}
			startMacro.at = mm.Replace("\")\\asm(\"", "\n");
		} else {
			Macro temp = startMacro;
			while(temp.nextMacro != null){
				temp = temp.nextMacro;
			}
			temp.nextMacro = new Macro();
			int name = macros[0].IndexOf(" ");
			temp.nextMacro.nm = macros[0].Substring(0, name).Replace("%a", " ").Replace("%b", " ").Replace("%c", " ").Replace("%d", " ").Trim().Split(" ");
			// for(int p = 0; p < startMacro.nm.Length; ++p) Console.WriteLine(startMacro.nm[p]);
			temp.nextMacro.mt = macros[0];
			string mm = macros[1];
			for(int i = 2; i < macros.Length; ++i){
				mm += "asm(\"";
				mm += macros[i];
				temp.nextMacro.hard = true;
			}
			temp.nextMacro.at = mm.Replace("\")\\asm(\"", "\n");
		}
	}

	public string ImplementMacro(Macro macro, string param){
		string a = "", b = "", c = "", d = "";
		string p = param.Replace(" ", "");
		string prtr = macro.mt;
		for(int i = 0; i < macro.nm.Length; ++i){
			p = p.Replace(macro.nm[i], " ");
			prtr = prtr.Replace(macro.nm[i], " ");
		}
		string[] prm = p.Split(" ");
		string[] prnt = prtr.Split(" ");

		for(int k = 0; k < prm.Length; ++k){
			switch(prnt[k]){
				case "%a":
					a = prm[k];
					break;

				case "%b":
					b = prm[k];
					break;

				case "%c":
					c = prm[k];
					break;

				case "%d":
					d = prm[k];
					break;
			}
		}
		string mac = macro.at.Replace("%a", a).Replace("%b", b).Replace("%c", c).Replace("%d", d);
		// // Console.WriteLine(mac);
		return mac;
	}
}

//[=, +] 
// %c=%a+%b
// asm("mov %c, %a")\asm("add %c, %b")
//mov %c, %a")\asm("add %c, %b