\ xorshift64 PRNG 
\ ref: https://en.wikipedia.org/wiki/Xorshift


variable SEED 


: randomize 
    utime drop seed ! 
; 

\ implement xorshift64
: rnd ( -- n+ )
    seed @ DUP 13 LSHIFT XOR
    DUP 7 RSHIFT XOR 
    DUP 17 LSHIFT XOR 
    DUP SEED ! 
    -1 1 RSHIFT AND \ >=0 
; 

: random ( n -- {0..n-1} )
    rnd swap mod 
; 

randomize
