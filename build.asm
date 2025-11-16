[BITS 16]
in al, 92h
or al, 2
out 92h, al
nop
mov eax, 0
int 10h
mov ebx, 0B8000h
mov al, 70
add al, 2
mov [ebx], al
mov [ebx+1], al
times 510-($-$$) db 0
dw 0AA55h