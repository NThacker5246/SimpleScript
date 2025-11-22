[BITS 16]
in al, 92h
or al, 2
out 92h, al
nop
xor eax, eax
int 10h
movement dd 0
charable dd 0
mov eax, 4
mov [movement], eax
mov eax, 65
mov [charable], eax
mov ebx, [movement]
push ebx
mov ebx, [charable]
push ebx
push ebp
mov ebp, esp
call printchar
printchar:
mov ax, [ebp+8]
shl ax, 1
mov ebx, 0B8000h
add ebx, eax
mov al, [ebp+4]
mov [ebx], al
ret
times 510-($-$$) db 0
dw 0AA55h