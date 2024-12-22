label startmod:
    define nc = DynamicCharacter('Natsuki Clone', image='clonesuki', what_prefix='"', what_suffix='"', ctc="ctc", ctc_position="fixed")
    # image clonesuki 1a = "im.Composite((960, 960), (0, 0), "clonesuki/1l.png", (0, 0), "clonesuki/1r.png", (0, 0), "clonesuki/a.png")"
    # define custom_music = "custom_music.ogg"
    # image bg bg_test2 = "bg_test_original/bg_test2.png"
    # image bg normal_fucking_background = "normal_fucking_background/normal_fucking_background.jpg"
    stop music fadeout 2.0
    play music p2
    # play music custom_music

    scene bg corridor
    # scene bg bg_test2
    with dissolve_scene_full
    "As I make my way through the hall, I can't help but notice some frustrated sighs coming from the clubroom."
    scene bg club_day
    # scene bg normal_fucking_background
    #show natsuki 3o zorder 1 at t11
    #show natsuki2 3o zorder 2 at h34
    show natsuki 3o zorder 2 at t22
    # nc "This is a test I'm a clone!"
    # show clonesuki 1a zorder 3 at f21
    n "Ugh why won't it work?"
    hide natsuki
    scene bg corridor
    "Through the window of the door I notice natsuki standing next to what looks like some big contraption."
    "What the hell is that girl up to? First cupcakes and now engineering? Has she lost her mind?"
    scene bg club_day
    with dissolve_scene_full
    
    mc "Hey Natsuki, what are you doing?"
    show natsuki 1p zorder 1 at h43
    n "OH SHIT-"
    hide natsuki
    show natsuki 3i zorder 1 at h43
    n "God.. don't do that dummy, you scared me."
    n "I'm currently trying to get the cloninator 9000 to work.."
    mc "Cloninator?"
    hide natsuki
    show natsuki 3j zorder 1 at t11
    n "Yep! Made it myself. Basically, this bad boy takes me and makes another me - I did it for help with like baking and stuff"
    hide natsuki
    show natsuki 2q zorder 1 at t11
    n "Although.. It doesn't seem to be working right now.."
    mc "I see. Have you tried pushing the on button?"
    
    $ persistent.playthrough = 0

    jump main_menu

label startmod2:
    image bg bg_test2 = "bgextended/bg_test_original/bg_test2.png"

    define nc = DynamicCharacter('Natsuki Clone', image='natsuki', what_prefix='"', what_suffix='"', ctc="ctc", ctc_position="fixed")
    define custom_music = "<loop 0.70>custom_music.ogg"

    stop music fadeout 2.0
    play music custom_music

    $ s_name = "Sayori"

    scene bg bg_test2

    show sayori 4p zorder 2 at t11

    s "Gahh!!!! Where is it???"
    stop music fadeout 2.0
    play music t2
    nc "This is a test!"
    mc "What's up Sayori?"

    hide sayori
    show sayori 1u zorder 2 at h43
    s "I can't find my favorite sweater!"
    "Sweater? Do you even hear yourself right now? No one cares.. seriously.."
    mc "Ah.. that's too bad"
    "I stuff the sweater violently into my backpack before Sayori can catch a glimpse at what I'm doing"

    call end_game