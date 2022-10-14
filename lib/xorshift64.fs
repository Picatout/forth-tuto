\ générateur pseudo aléatoire base sur l'algorithme
\ XORSHIFT 

\ valeur transitoire du générateur
variable SEED 

\ initialise le générateur
\ avec la valeur de la variable 
\ système utime
: randomize 
    utime drop seed ! 
; 

\ implemente xorshift64 selon 
\ https://en.wikipedia.org/wiki/Xorshift
\ retourne un entier positif {1..2^63}
: rnd ( -- n+ )
    seed @ DUP 13 LSHIFT XOR
    DUP 7 RSHIFT XOR 
    DUP 17 LSHIFT XOR 
    DUP SEED ! 
    -1 1 RSHIFT AND \ >=0 
; 

\ retourne un entier positif 
\ dans l'intervale modulo 'n'
: random ( n -- {0..n-1} )
    rnd swap mod 
; 

randomize
